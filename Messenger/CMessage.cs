using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Documents;
using System.Windows.Media;

namespace Messenger {
    enum EStatus {
        Sending,
        Sent,
        FailedToSend,
        Delivered,
        Seen
    }
    class CMessage {
        private string m_sender;
        private string m_user;
        private string m_content;
        private TextRange m_message_range;
        private TextRange m_nick_range;
        DateTime m_time;
        public EStatus status { get; set; }
        
        public CMessage(string user, string sender, ref byte[] msg_content, CModel.EMessageType msg_type, EStatus msg_status, TextPointer msg_begin, DateTime time) {
            m_sender = sender;
            m_user = user;
            if (msg_type == CModel.EMessageType.Text)
                m_content = System.Text.Encoding.UTF8.GetString(msg_content) + "\r";
            else if (msg_type == CModel.EMessageType.Image)
                m_content = m_sender + " sent an image\r";
            else
                m_content = m_sender + " sent a video\r";
            status = msg_status;
            m_nick_range.Select(msg_begin, msg_begin.GetPositionAtOffset(sender.Length + 2));
            m_nick_range.Text = sender + ": ";
            m_message_range.Select(msg_begin.GetPositionAtOffset(sender.Length + 2), msg_begin.GetPositionAtOffset(m_content.Length + sender.Length + 2));
            m_message_range.Text = m_content;
            m_time = time;
        }
        public string Content() {
            return m_content;
        }
        public void Represent() {
            BrushConverter bc = new BrushConverter();
            if (m_sender == m_user) {
                m_nick_range.ApplyPropertyValue(TextElement.ForegroundProperty, bc.ConvertFromString("Red"));
            }
            else {
                m_nick_range.ApplyPropertyValue(TextElement.ForegroundProperty, bc.ConvertFromString("Blue"));
            }
        }
    }
}
