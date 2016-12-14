using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace Messenger {
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {
        private CModel m_model;
        private const long MAX_FILE_SIZE = 209715200; //200 MB
        private const int REQUEST_USERS_PERIOD = 5;
        private DispatcherTimer m_request_users_timer;
        public MainWindow() {
            InitializeComponent();
            m_model = new CModel(new CModel.UpdateUserListDelegate(UpdateUserList), 
                new CModel.GetMessageDocumentEnd(GetMessageDocumentEnd),
                new CModel.IncomingMessage(IncomingMessage),
                new CModel.SaveIncomingFile(IncomingFile));
            m_request_users_timer = new DispatcherTimer();
            m_request_users_timer.Tick += new EventHandler(Request_Users);
            m_request_users_timer.Interval = new TimeSpan(0, 0, REQUEST_USERS_PERIOD);
        }
        public TextPointer GetMessageDocumentEnd() {
            return this.output_textbox.Document.ContentEnd;
        }
        public void IncomingMessage(string msg_id) {
            if (this.message_input_textbox.IsFocused) {
                m_model.SendMessageSeen(msg_id);
            }
            else {
                m_model.AddUnreadMessage(msg_id);
                MessengerWindow.Title += " <New messages>";
            }
        }
        public bool IncomingFile(string sender, bool is_image, out string filename) {
            filename = "";
            string caption = sender + " sent you " + (is_image ? "an image." : "a video.") + " Do you want to save this file?";
            var res = MessageBox.Show(caption, "Incoming file", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (res == MessageBoxResult.Yes) {
                SaveFileDialog fileDlg = new SaveFileDialog();
                fileDlg.InitialDirectory = "c:\\";
                bool? dlg_res = fileDlg.ShowDialog();
                if (dlg_res == true) {
                    filename = fileDlg.FileName;
                    return true;
                }
            }
            return false;
        }
        private void UpdateUserList(List<string> user_list) {
            this.user_listbox.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => {
                this.user_listbox.Items.Clear();
                foreach (string user in user_list) {
                    this.user_listbox.Items.Add(user);
                }
            }));
        }
        private void Request_Users(object sender, EventArgs e) {
            m_model.RequestActiveUsers();
        }
        private void _LoginWindow(out string user_id, out string password, out string server_address, out ushort port, out bool use_encryption) {
            LoginWindow window = new LoginWindow();
            window.ShowDialog();
            user_id = window.user_id;
            password = window.password;
            server_address = window.server_address;
            port = window.port;
            use_encryption = window.encryption_enabled;
        }
        private void _SendMessage(string message) {
            byte[] msg_arr = System.Text.Encoding.UTF8.GetBytes(message);
            m_model.EnqueueEvent(new CQueueMessage(m_model.m_user_id, 1, m_model.m_user_id, msg_arr, null,
                1, output_textbox.Document.ContentEnd));
        }
        private void Send_Click(object sender, RoutedEventArgs e) {
            if (!m_model.m_is_logged_in) {
                string user_id, password, server_address;
                ushort port;
                bool use_encryption;
                _LoginWindow(out user_id, out password, out server_address, out port, out use_encryption);
                m_model.Login(user_id, password, server_address, port, use_encryption);
                while (m_model.WasLoginProbe() == false) continue;
                if (m_model.m_is_logged_in) {
                    MessengerWindow.login_button.Content = "Send";
                    MessengerWindow.send_file_button.IsEnabled = true;
                    MessengerWindow.message_input_textbox.IsEnabled = true;
                    m_request_users_timer.Start();
                    Task.Run(() => { m_model.ProcessEvents(); });
                }
                else {
                    m_model.ResetLoginProbe();
                    MessageBox.Show(m_model.GetLoginError(), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else {
                string message_text = this.message_input_textbox.Text;
                _SendMessage(message_text);
            }
            this.message_input_textbox.Clear();
        }
        private void Attach_Click(object sender, RoutedEventArgs e) {
            OpenFileDialog fileDialog = new OpenFileDialog();
            fileDialog.InitialDirectory = "c:\\";
            fileDialog.Filter = "Image Files(*.bmp;*.jpg;*.gif)|*.bmp;*.jpg;*.gif|Video files(*.avi, *.mkv, *.mp4)|*.avi;*.mkv;*.mp4";
            fileDialog.FilterIndex = 1;
            bool? res = fileDialog.ShowDialog();
            if (res == true) {
                string filename = fileDialog.FileName;
                long filesize = new System.IO.FileInfo(filename).Length;
                if (filesize > MAX_FILE_SIZE) {
                    MessageBox.Show("Max file size is 200MB", "Too large file", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                Char delimiter = '.';
                var string_list = filename.Split(delimiter);
                string filetype = string_list[string_list.Length - 1];
                CModel.EMessageType message_type = CModel.EMessageType.Image;
                if (filename == "avi" || filename == "mkv" || filename == "mp4")
                    message_type = CModel.EMessageType.Video;
                byte[] file_content = File.ReadAllBytes(filename);
                //m_model.SendMessage(ref file_content, message_type, output_textbox.Document.ContentEnd);
            }
        }
        private void message_input_textbox_KeyDown(object sender, KeyEventArgs e) {
            if (e.Key == Key.Enter && !Keyboard.IsKeyDown(Key.LeftCtrl)) {
                this.login_button.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
            }
            else if (e.Key == Key.Enter && Keyboard.IsKeyDown(Key.LeftCtrl)) {
                message_input_textbox.Text += "\r";
            }
        }
        private void MessengerWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e) {
            m_model.CloseConnection();
        }
        private void message_input_textbox_GotFocus(object sender, RoutedEventArgs e) {
            MessengerWindow.Title = "Messenger";
            m_model.AllMessagesSeen();
        }
    }
}
