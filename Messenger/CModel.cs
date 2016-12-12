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
        private Dictionary<string, CMessage> m_messages;
        public CModel() {
            m_is_logged_in = false;
            m_messages = new Dictionary<string, CMessage>();
        }
        private CBackendController m_backend;
        private bool m_encryption;
        public bool m_is_logged_in { get; set; }
        public string m_user_id { get; set; }
        public void Login(string user_id, string password, string server_address, ushort port, bool use_encryption) {
            m_user_id = user_id;
            m_encryption = use_encryption;
            m_backend = new CBackendController(server_address, port);
            m_backend.Login(user_id, password, use_encryption);
            m_is_logged_in = true; //TODO: only if successfully logged in
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
