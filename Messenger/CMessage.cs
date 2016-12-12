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
        private TextRange m_date_range;
        DateTime m_time;
        public EStatus status { get; set; }
        public CMessage(string user, string sender, ref byte[] msg_content, CModel.EMessageType msg_type, EStatus msg_status, TextPointer doc_end, DateTime time) {
            TextRange content_range = new TextRange(doc_end, doc_end);
            m_sender = sender;
            m_user = user;
            string msg_str;
            string date_str = "<" + time.ToString() + ">";
            if (msg_type == CModel.EMessageType.Text) {
                msg_str = System.Text.Encoding.UTF8.GetString(msg_content);;
                m_content = date_str + m_sender + ": " + msg_str + "\r";
                
            }
            else if (msg_type == CModel.EMessageType.Image) {
                msg_str = " sent an image\r";
                m_content = date_str + m_sender + msg_str;
            }
            else {
                msg_str = " sent a video\r";
                m_content = date_str + m_sender + msg_str;
            }
            content_range.Text = m_content;
            m_date_range = new TextRange(content_range.Start, content_range.Start.GetPositionAtOffset(date_str.Length));
            m_nick_range = new TextRange(m_date_range.End, m_date_range.End.GetPositionAtOffset(m_sender.Length + 1));
            m_message_range = new TextRange(m_nick_range.End, m_nick_range.End.GetPositionAtOffset(msg_str.Length));
            status = msg_status;
            UpdateRepresentation();
            m_time = time;
        }
        public void UpdateRepresentation() {
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
