using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Threading;
using System.Collections.Concurrent;

namespace Messenger {
    class CSignatures {
        public const string GIF_SIGNATURE_1 = "474946383761";
        public const string GIF_SIGNATURE_2 = "474946383961";
        public const string BMP_SIGNATURE = "424D";
        //public const string MP3_SIGNATURE_1 = "FFFB";
        //public const string MP3_SIGNATURE_2 = "494433";
        public const string PNG_SIGNATURE = "89504E470D0A1A0A";
        public const string OGG_SIGNATURE = "4F676753";
    }
    class CQueueMessage {
        public enum EType {
            StatusUpdated,
            SendRecv
        }
        public string m_user_id { get; set; }
        public string m_sender_id { get; set; }
        public byte[] m_data { get; set; }
        public string m_message_id { get; set; }
        public int m_status { get; set; }
        public int m_type { get; set; }
        public TextPointer m_text_ptr { get; set; }
        public DateTime m_date { get; set; }
        public CQueueMessage(string user_id, int status, string sender_id = null, byte[] data = null, string message_id = null, 
            int type = -1, TextPointer text_ptr = null, DateTime date = new DateTime()) {
            m_user_id = user_id;
            m_status = status;
            m_sender_id = sender_id;
            m_data = data;
            m_message_id = message_id;
            m_text_ptr = text_ptr;
            m_date = date;
            m_type = type;
        }
        public EType Type() {
            if (m_sender_id == null) return EType.StatusUpdated;
            else return EType.SendRecv;
        }
    }
    class CModel {
        public enum EMessageType {
            Text,
            Image, 
            Video
        }
        public enum ERequestStatus {
            Ok,
            AuthError,
            NetworkError,
            InternalError
        }
        ConcurrentQueue<CQueueMessage> m_event_queue;

        private Dictionary<string, CMessage> m_messages;
        private List<string> m_new_messages;
        private List<string> m_users;
        private CBackendController m_backend;
        private ERequestStatus m_user_request_status;
        private ERequestStatus m_login_request_status;
        private bool m_login_probe;
        private UpdateUserListDelegate m_update_user_list_delegate;
        private GetMessageDocumentEnd m_get_message_document_end;
        private IncomingMessage m_incoming_message;
        private SaveIncomingFile m_incoming_file;
        private SynchronizationContext m_context;
        public CModel(UpdateUserListDelegate upd_delegate, GetMessageDocumentEnd get_msg_doc_delegate,
            IncomingMessage incoming_message_delegate, SaveIncomingFile incoming_file_delegate, SynchronizationContext cntx) {
            m_is_logged_in = false;
            m_messages = new Dictionary<string, CMessage>();
            m_new_messages = new List<string>();
            m_login_probe = false;
            m_update_user_list_delegate = upd_delegate;
            m_get_message_document_end = get_msg_doc_delegate;
            m_incoming_message = incoming_message_delegate;
            m_incoming_file = incoming_file_delegate;
            m_event_queue = new ConcurrentQueue<CQueueMessage>();
            m_context = cntx;
        }
        public delegate void UpdateUserListDelegate(List<string> user_list);
        public delegate TextPointer GetMessageDocumentEnd();
        public delegate void IncomingMessage(string msg_id);
        public delegate bool SaveIncomingFile(string sender, bool is_image, out string filename, string file_type);
        public bool m_is_logged_in { get; set; }
        public string m_user_id { get; set; }
        public void EnqueueEvent(CQueueMessage queue_msg) {
            m_event_queue.Enqueue(queue_msg);
        }
        public void ProcessEvents() {
            while (true) {
                while (!m_event_queue.IsEmpty) {
                    CQueueMessage msg;
                    m_event_queue.TryDequeue(out msg);
                    if (msg.Type() == CQueueMessage.EType.StatusUpdated) _ProcessUpdatedStatus(msg);
                    else _ProcessSendRecv(msg);
                }
            }
        }
        private void _ProcessUpdatedStatus(CQueueMessage queue_msg) {
            CMessage message;
            if (m_messages.ContainsKey(queue_msg.m_user_id))
                message = m_messages[queue_msg.m_user_id];
            else return;
            switch (queue_msg.m_status) {
                case 0:         //Sending
                    message.UpdateRepresentation(EStatus.Sending, m_context);
                    break;
                case 1:         //Sent
                    message.UpdateRepresentation(EStatus.Sent, m_context);
                    break;
                case 2:         //Failed to send
                    message.UpdateRepresentation(EStatus.FailedToSend, m_context);
                    break;
                case 3:         //Delivered
                    message.UpdateRepresentation(EStatus.Delivered, m_context);
                    break;
                case 4:         //Seen
                    message.UpdateRepresentation(EStatus.Seen, m_context);
                    m_messages.Remove(queue_msg.m_user_id);
                    break;
            }
        }
        private void _ProcessSendRecv(CQueueMessage queue_msg) {
            if (queue_msg.m_status == 5) _ProcessRecv(queue_msg);
            else _ProcessSend(queue_msg);
        }
        private void _ProcessSend(CQueueMessage queue_msg) {
            m_backend.SendMessage(queue_msg.m_user_id, queue_msg.m_data, queue_msg.m_type);
            string key = m_backend.GetLastMessageId();
            DateTime msg_date = m_backend.GetLastMessageDate();
            EMessageType msg_type = EMessageType.Text;
            switch(queue_msg.m_type) {
                case 1:
                    msg_type = EMessageType.Text;
                    break;
                case 2:
                    msg_type = EMessageType.Image;
                    break;
                case 3:
                    msg_type = EMessageType.Video;
                    break;
            }
            CMessage msg = new CMessage(m_user_id, m_user_id, queue_msg.m_data, msg_type, EStatus.Sending, 
                queue_msg.m_text_ptr, msg_date, m_context);
            m_messages.Add(key, msg);
        }

        private void _ProcessRecv(CQueueMessage queue_msg) {
            EMessageType msg_type = EMessageType.Text;
            switch (queue_msg.m_type) {
                case 0:
                    msg_type = EMessageType.Text;
                    break;
                case 1:
                    msg_type = EMessageType.Image;
                    break;
                case 2:
                    msg_type = EMessageType.Video;
                    break;
            }
            CMessage msg = new CMessage(queue_msg.m_user_id, queue_msg.m_sender_id, queue_msg.m_data, msg_type,
                EStatus.Incoming, queue_msg.m_text_ptr, queue_msg.m_date, m_context);
            m_messages.Add(queue_msg.m_message_id, msg);
            if (msg_type != EMessageType.Text) {
                string file_type = _GetFileType(queue_msg.m_data);
                string filename;
                if (m_incoming_file(queue_msg.m_sender_id, msg_type == EMessageType.Image ? true : false, out filename, file_type))
                    File.WriteAllBytes(filename, queue_msg.m_data);
            }
            m_incoming_message(queue_msg.m_message_id);
        }
        private bool _CheckGIF(ref byte[] file_content) {
            if (file_content.Length < 6) return false;
            else {
                byte[] signature = { file_content[0], file_content[1], file_content[2], file_content[3], file_content[4], file_content[5] };
                if (BitConverter.ToString(signature).Replace("-", string.Empty) == CSignatures.GIF_SIGNATURE_1
                    || BitConverter.ToString(signature).Replace("-", string.Empty) == CSignatures.GIF_SIGNATURE_2)
                    return true;
            }
            return false;
        }
        private bool _CheckBMP(ref byte[] file_content) {
            if (file_content.Length < 2) return false;
            else {
                byte[] signature = { file_content[0], file_content[1] };
                if (BitConverter.ToString(signature).Replace("-", string.Empty) == CSignatures.BMP_SIGNATURE)
                    return true;
            }
            return false;
        }
        private bool _CheckPNG(ref byte[] file_content) {
            if(file_content.Length < 8) return false;
            else {
                byte[] signature = { file_content[0], file_content[1], file_content[2], file_content[3], file_content[4],
                                       file_content[5], file_content[6], file_content[7] };
                if (BitConverter.ToString(signature).Replace("-", string.Empty) == CSignatures.PNG_SIGNATURE)
                    return true;
            }
            return false;
        }
        private bool _CheckOGG(ref byte[] file_content) {
            if(file_content.Length < 4) return false;
            else {
                byte[] signature = { file_content[0], file_content[1], file_content[2], file_content[3] };
                if (BitConverter.ToString(signature).Replace("-", string.Empty) == CSignatures.OGG_SIGNATURE)
                    return true;
            }
            return false;
        }
        private string _GetFileType(byte[] file_content) {
            if (_CheckGIF(ref file_content)) return "gif";
            else if (_CheckBMP(ref file_content)) return "bmp";
            else if (_CheckPNG(ref file_content)) return "png";
            else if (_CheckOGG(ref file_content)) return "ogg";
            else return null;
        }
        public void Login(string user_id, string password, string server_address, ushort port, bool use_encryption) {
            m_user_id = user_id;
            m_backend = new CBackendController(server_address, port, 
                new CBackendController.RequestUsersCallBack(ProcessUserRequestStatus),
                new CBackendController.LoginRequestCallback(ProcessLoginRequestStatus),
                new CBackendController.MessageStatusChangeCallback(OnMessageStatusChange),
                new CBackendController.MessageReceivedCallback(OnMessageReceived));
            m_backend.Login(user_id, password, use_encryption);
        }
        public void RequestActiveUsers() {
            m_backend.RequestActiveUsers();
        }
        public string GetLoginError() {
            if (m_login_request_status == ERequestStatus.AuthError)
                return "Authorisation error";
            else if (m_login_request_status == ERequestStatus.InternalError)
                return "Internal error";
            else return "Network error";
        }
        private void _SetStatus(int status, ref ERequestStatus request_status) {
            switch (status) {
                case 0:         //Ok
                    request_status = ERequestStatus.Ok;
                    break;
                case 1:         //AuthError
                    request_status = ERequestStatus.AuthError;
                    break;
                case 2:         //NetworkError
                    request_status = ERequestStatus.NetworkError;
                    break;
                case 3:         //InternalError
                    request_status = ERequestStatus.InternalError;
                    break;
            }
        }
        public void ProcessLoginRequestStatus(int status) {
            _SetStatus(status, ref m_login_request_status);
            m_login_probe = true;
            if (m_login_request_status == ERequestStatus.Ok)
                m_is_logged_in = true;
        }
        public void ProcessUserRequestStatus(int status) {
            _SetStatus(status, ref m_user_request_status);
            if (m_user_request_status == ERequestStatus.Ok) {
                m_users = m_backend.GetUserList();
            }
            else if (m_user_request_status == ERequestStatus.AuthError) {
                m_users = new List<string>();
                m_users.Add("Authorisation error");
            }
            else if (m_user_request_status == ERequestStatus.InternalError) {
                m_users = new List<string>();
                m_users.Add("Internal error");
            }
            else {
                m_users = new List<string>();
                m_users.Add("Network error");
            }
            m_update_user_list_delegate(m_users);
        }
        public void OnMessageStatusChange(IntPtr msg_id, int str_len, int status) {
            byte[] msg_id_in_bytes = new byte[str_len];
            Marshal.Copy(msg_id, msg_id_in_bytes, 0, str_len);
            string key = Encoding.ASCII.GetString(msg_id_in_bytes);
           EnqueueEvent(new CQueueMessage(key, status));
        }
        public void OnMessageReceived(IntPtr user_id, int user_id_len, IntPtr msg_id, int msg_id_len, 
            int time, int type, IntPtr data_ptr, int data_size) {
            byte[] usr_id_in_bytes = new byte[user_id_len];
            Marshal.Copy(user_id, usr_id_in_bytes, 0, user_id_len);
            string uid = Encoding.UTF8.GetString(usr_id_in_bytes);

            byte[] msg_id_in_bytes = new byte[msg_id_len];
            Marshal.Copy(msg_id, msg_id_in_bytes, 0, msg_id_len);
            string message_id = Encoding.ASCII.GetString(msg_id_in_bytes);

            byte[] data = new byte[data_size];
            if(data_ptr != IntPtr.Zero) Marshal.Copy(data_ptr, data, 0, data_size);
            m_backend.FreePtr(data_ptr);
            EnqueueEvent(new CQueueMessage(m_user_id, 5, uid, data, message_id, type, m_get_message_document_end(),
                new DateTime(1970, 1, 1).ToLocalTime().AddSeconds(time)));
        }
        public bool WasLoginProbe() {
            return m_login_probe;
        }
        public void ResetLoginProbe() {
            m_login_probe = false;
            m_is_logged_in = false;
        }
        public void AddUnreadMessage(string id) {
            m_new_messages.Add(id);
        }
        public void CloseConnection() {
            if(m_is_logged_in) m_backend.Disconnect();
        }
        public void SendMessageSeen(string id) {
            m_backend.SendMessageSeen(m_messages[id].m_sender, id);
            m_messages[id].UpdateRepresentation(EStatus.Seen, m_context);
            m_messages.Remove(id);
        }
        public void AllMessagesSeen() {
            foreach (string id in m_new_messages) 
                SendMessageSeen(id);
            m_new_messages.Clear();
        }
    }
}
