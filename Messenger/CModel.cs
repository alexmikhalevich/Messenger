using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Documents;

namespace Messenger {
    class CModel {
        public enum EMessageType {
            Text,
            Image, 
            Video
        }
        public enum EUserRequestStatus {
            Ok,
            AuthError,
            NetworkError,
            InternalError
        }
        private Dictionary<string, CMessage> m_messages;
        public CModel() {
            m_is_logged_in = false;
            m_messages = new Dictionary<string, CMessage>();
        }
        private CBackendController m_backend;
        private List<string> m_users;
        EUserRequestStatus m_user_request_status;
        private bool m_encryption;
        public bool m_is_logged_in { get; set; }
        public string m_user_id { get; set; }
        public void Login(string user_id, string password, string server_address, ushort port, bool use_encryption) {
            m_user_id = user_id;
            m_encryption = use_encryption;
            m_backend = new CBackendController(server_address, port, new CBackendController.RequestUsersCallBack(ProcessUserRequestStatus));
            m_backend.Login(user_id, password, use_encryption);
            m_is_logged_in = true; //TODO: only if successfully logged in
        }
        public void ProcessUserRequestStatus(int status) {
            switch (status) {
                case 1:         //Ok
                    m_users = m_backend.GetUserList();
                    m_user_request_status = EUserRequestStatus.Ok;
                    break;
                case 2:         //AuthError
                    m_user_request_status = EUserRequestStatus.AuthError;
                    break;
                case 3:         //NetworkError
                    m_user_request_status = EUserRequestStatus.NetworkError;
                    break;
                case 4:         //InternalError
                    m_user_request_status = EUserRequestStatus.InternalError;
                    break;
            }
        }
        public void GetUserList(out List<string> user_list) {
            if (m_user_request_status == EUserRequestStatus.Ok)
                user_list = m_users;
            else if (m_user_request_status == EUserRequestStatus.AuthError) {
                user_list = new List<string>();
                user_list.Add("Authorisation error");
            }
            else if (m_user_request_status == EUserRequestStatus.InternalError) {
                user_list = new List<string>();
                user_list.Add("Internal error");
            }
            else {
                user_list = new List<string>();
                user_list.Add("Network error");
            }
        }
        public CMessage SendMessage(ref byte[] message, EMessageType message_type, TextPointer msg_pointer) {
            int type = 1;
            switch (message_type) {
                case EMessageType.Text:
                    type = 1;
                    break;
                case EMessageType.Image:
                    type = 2;
                    break;
                case EMessageType.Video:
                    type = 3;
                    break;
            }
            m_backend.SendMessage(m_user_id, ref message, m_encryption, type);
            string key = m_backend.GetLastMessageId();
            DateTime msg_date = m_backend.GetLastMessageDate();
            CMessage msg = new CMessage(m_user_id, m_user_id, ref message, message_type, EStatus.Sending, msg_pointer, msg_date);
            m_messages.Add(key, msg);
            return msg;
        }
        public void CloseConnection() {
            m_backend.Disconnect();
        }
        public CMessage GetMessageById(string id) {
            return m_messages[id];
        }
        public void AllMessagesSeen() {

        }
    }
}
