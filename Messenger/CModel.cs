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
        public enum ERequestStatus {
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
        ERequestStatus m_user_request_status;
        ERequestStatus m_login_request_status;
        private bool m_encryption;
        public bool m_is_logged_in { get; set; }
        public string m_user_id { get; set; }
        public void Login(string user_id, string password, string server_address, ushort port, bool use_encryption) {
            m_user_id = user_id;
            m_encryption = use_encryption;
            m_backend = new CBackendController(server_address, port, 
                new CBackendController.RequestUsersCallBack(ProcessUserRequestStatus),
                new CBackendController.LoginRequestCallback(ProcessLoginRequestStatus));
            m_backend.Login(user_id, password, use_encryption);
        }
        public void RequestActiveUsers(out List<string> user_list) {
            m_backend.RequestActiveUsers();
            if (m_user_request_status == ERequestStatus.Ok)
                user_list = m_users;
            else if (m_user_request_status == ERequestStatus.AuthError) {
                user_list = new List<string>();
                user_list.Add("Authorisation error");
            }
            else if (m_user_request_status == ERequestStatus.InternalError) {
                user_list = new List<string>();
                user_list.Add("Internal error");
            }
            else {
                user_list = new List<string>();
                user_list.Add("Network error");
            }
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
                    m_users = m_backend.GetUserList();
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
            if (m_login_request_status == ERequestStatus.Ok)
                m_is_logged_in = true;
        }
        public void ProcessUserRequestStatus(int status) {
            _SetStatus(status, ref m_user_request_status);
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
