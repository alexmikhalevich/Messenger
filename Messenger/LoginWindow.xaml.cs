using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Messenger {
    /// <summary>
    /// Логика взаимодействия для LoginWindow.xaml
    /// </summary>
    public partial class LoginWindow : Window {
        public LoginWindow() {
            InitializeComponent();
            correct_input = false;
        }
        public string user_id { get; set; }
        public string password { get; set; }
        public string server_address { get; set; }
        public ushort port { get; set; }
        public bool encryption_enabled { get; set; }
        public bool correct_input { get; set; }
        private void Button_Click(object sender, RoutedEventArgs e) {
            if (!login_textbox.Text.Contains("@")) {
                MessageBox.Show("Invalid login format", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            user_id = this.login_textbox.Text;
            password = this.password_textbox.Text;
            if(!Regex.IsMatch(server_textbox.Text,
                @"^(?:(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.){3}(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)$")) {
                MessageBox.Show("Invalid address format", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            server_address = this.server_textbox.Text;
            if (this.encryption_checkbox.IsChecked == null || this.encryption_checkbox.IsChecked == false)
                encryption_enabled = false;
            else encryption_enabled = true;
            ushort server_port;
            bool res = ushort.TryParse(this.port_textbox.Text, out server_port);
            if (!res) {
                MessageBox.Show("Invalid port", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            else {
                port = server_port;
                correct_input = true;
                this.LoginWindow1.Close();
            }
        }
        private void login_textbox_GotFocus(object sender, RoutedEventArgs e) {
            this.login_textbox.Clear();
        }

        private void password_textbox_GotFocus(object sender, RoutedEventArgs e) {
            this.password_textbox.Clear();
        }

        private void server_textbox_GotFocus(object sender, RoutedEventArgs e) {
            this.server_textbox.Clear();
        }
    }
}
