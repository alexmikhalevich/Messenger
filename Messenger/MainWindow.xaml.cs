using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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
        private const long MAX_FILE_SIZE = 50000000; //50 MB
        private const int REQUEST_USERS_PERIOD = 5;
        private DispatcherTimer m_request_users_timer;
        private string m_destination;
        public MainWindow() {
            InitializeComponent();
            m_model = new CModel(new CModel.UpdateUserListDelegate(UpdateUserList), 
                new CModel.GetMessageDocumentEnd(GetMessageDocumentEnd),
                new CModel.IncomingMessage(IncomingMessage),
                new CModel.SaveIncomingFile(IncomingFile),
                new CModel.LoginRequestResultProcessor(LoginResponseProcessor),
                SynchronizationContext.Current);
            m_request_users_timer = new DispatcherTimer();
            m_request_users_timer.Tick += new EventHandler(Request_Users);
            m_request_users_timer.Interval = new TimeSpan(0, 0, REQUEST_USERS_PERIOD);
            m_destination = null;

            if (File.Exists("history")) {
                var res = MessageBox.Show("Do you want to load your history?", "History", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (res == MessageBoxResult.Yes) {
                    TextRange content = new TextRange(output_textbox.Document.ContentEnd, output_textbox.Document.ContentEnd);
                    using (FileStream fs = File.Open("history", FileMode.Open)) {
                        content.Load(fs, DataFormats.Xaml);
                    }
                }
            }
        }
        public TextPointer GetMessageDocumentEnd() {
            return this.output_textbox.Document.ContentEnd;
        }
        public void IncomingMessage(string msg_id) {
            bool is_focused = false;
            this.message_input_textbox.Dispatcher.Invoke(() => { is_focused = this.message_input_textbox.IsFocused; });
            if (is_focused) m_model.SendMessageSeen(msg_id);
            else {
                m_model.AddUnreadMessage(msg_id);
                MessengerWindow.Dispatcher.Invoke(() => { MessengerWindow.Title += " <New messages>"; });
            }
        }
        public bool IncomingFile(string sender, bool is_image, out string filename, string file_type) {
            filename = "";
            if (file_type == null) {
                MessageBox.Show(sender + " sent you file in unsupported format", "Incoming file",
                    MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return false;
            }
            string caption = sender + " sent you " + (is_image ? "an image." : "a video.") + " Do you want to save this file?";
            var res = MessageBox.Show(caption, "Incoming file", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (res == MessageBoxResult.Yes) {
                SaveFileDialog fileDlg = new SaveFileDialog();
                if (file_type == "ogg") fileDlg.Filter = "Video files | *.ogg";
                else fileDlg.Filter = "Image files | *." + file_type;
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
            if (!window.correct_input) {
                user_id = null;
                password = null;
                server_address = null;
                port = 0;
                use_encryption = window.encryption_enabled;
            }
            else {
                user_id = window.user_id;
                password = window.password;
                server_address = window.server_address;
                port = window.port;
                use_encryption = window.encryption_enabled;
            }
        }
        private void _SendMessage(string message) {
            byte[] msg_arr = System.Text.Encoding.UTF8.GetBytes(message);
            m_model.EnqueueEvent(new CQueueMessage(m_destination, 1, m_model.m_user_id, msg_arr, null,
                1, output_textbox.Document.ContentEnd));
        }
        private void Send_Click(object sender, RoutedEventArgs e) {
            if (!m_model.m_is_logged_in) {
                string user_id, password, server_address;
                ushort port;
                bool use_encryption;
                _LoginWindow(out user_id, out password, out server_address, out port, out use_encryption);
                if (user_id == null) return;
                m_model.Login(user_id, password, server_address, port, use_encryption);
            }
            else {
                string message_text = this.message_input_textbox.Text;
                if(m_destination != null) _SendMessage(message_text);
                else MessageBox.Show("You should choose destination user", "Warning", MessageBoxButton.OK, MessageBoxImage.Exclamation);
            }
            this.message_input_textbox.Clear();
        }
        public void LoginResponseProcessor() {
            if (m_model.m_is_logged_in) {
                MessengerWindow.Dispatcher.Invoke(() => { login_button.Content = "Send"; });
                MessengerWindow.Dispatcher.Invoke(() => { send_file_button.IsEnabled = true; });
                MessengerWindow.Dispatcher.Invoke(() => { message_input_textbox.IsEnabled = true; });
                m_model.RequestActiveUsers();
                m_request_users_timer.Start();
                Task.Run(() => { m_model.ProcessEvents(); });
            }
            else {
                m_model.ResetLoginProbe();
                MessageBox.Show(m_model.GetLoginError(), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void Attach_Click(object sender, RoutedEventArgs e) {
            OpenFileDialog fileDialog = new OpenFileDialog();
            fileDialog.InitialDirectory = "c:\\";
            fileDialog.Filter = "Image Files(*.bmp;*.gif;*.png)|*.bmp;*.gif;*.png|Video files(*.ogg)|*.ogg";
            fileDialog.FilterIndex = 1;
            bool? res = fileDialog.ShowDialog();
            if (res == true) {
                string filename = fileDialog.FileName;
                long filesize = new System.IO.FileInfo(filename).Length;
                if (filesize > MAX_FILE_SIZE) {
                    MessageBox.Show("Max file size is " + (MAX_FILE_SIZE / 1000000).ToString() + "MB", 
                        "Too large file", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                Char delimiter = '.';
                var string_list = filename.Split(delimiter);
                string filetype = string_list[string_list.Length - 1];
                int message_type = 2;
                if (filename == "ogg")
                    message_type = 3;
                byte[] file_content = File.ReadAllBytes(filename);
                if (m_destination != null) {
                    m_model.EnqueueEvent(new CQueueMessage(m_destination, 1, m_model.m_user_id, file_content, null,
                        message_type, output_textbox.Document.ContentEnd));
                }
                else MessageBox.Show("You should choose destination user", "Warning", MessageBoxButton.OK, MessageBoxImage.Exclamation);
            }
        }
        private void message_input_textbox_KeyDown(object sender, KeyEventArgs e) {
            if (e.Key == Key.Enter && !Keyboard.IsKeyDown(Key.LeftCtrl)) {
                this.login_button.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
            }
        }
        private void MessengerWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e) {
            TextRange content = new TextRange(output_textbox.Document.ContentStart, output_textbox.Document.ContentEnd);
            string s_content = content.Text;
            if (!String.IsNullOrWhiteSpace(s_content)) {
                var res = MessageBox.Show("Do you want to save your history?", "History", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (res == MessageBoxResult.Yes) {
                    content = new TextRange(output_textbox.Document.ContentStart, output_textbox.Document.ContentEnd);
                    using (FileStream fs = File.Create("history")) {
                        content.Save(fs, DataFormats.Xaml);
                    }
                }
            }
            m_model.CloseConnection();
        }
        private void message_input_textbox_GotFocus(object sender, RoutedEventArgs e) {
            MessengerWindow.Title = (m_destination == null) ? "Messenger" : "Destination: " + m_destination;
            m_model.AllMessagesSeen();
        }
        private void user_listbox_MouseDoubleClick(object sender, MouseButtonEventArgs e) {
            if (user_listbox.SelectedItem != null) {
                m_destination = user_listbox.SelectedItem.ToString();
                MessengerWindow.Title = "Destination: " + m_destination;
            }
        }
    }
}
