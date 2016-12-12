using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Messenger {
    class CBackendController : IDisposable {
        public delegate void RequestUsersCallBack(int status);
        public delegate void LoginRequestCallback(int status);
        public delegate void MessageStatusChangeCallback(IntPtr msg_id, int status);
        public delegate void MessageReceivedCallback(IntPtr user_id, IntPtr msg_id, int time, int type, byte[] data);
        #region PInvokes
        [DllImport("MessengerBackend.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
        static private extern IntPtr create_backend_instance([MarshalAs(UnmanagedType.LPStr)] string server_url, ushort port);
        [DllImport("MessengerBackend.dll", CallingConvention = CallingConvention.Cdecl)]
        static private extern void dispose_class(IntPtr pObject);
        [DllImport("MessengerBackend.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
        static private extern void call_login(IntPtr pObject, [MarshalAs(UnmanagedType.LPStr)] string user_id,
            [MarshalAs(UnmanagedType.LPStr)] string password, 
            [MarshalAs(UnmanagedType.Bool)] Boolean use_encryption,
            [MarshalAs(UnmanagedType.FunctionPtr)] LoginRequestCallback pfResult);
        [DllImport("MessengerBackend.dll", CallingConvention = CallingConvention.Cdecl)]
        static private extern void call_disconnect(IntPtr pObject);
        [DllImport("MessengerBackend.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
        static private extern void call_send_message(IntPtr pObject, [MarshalAs(UnmanagedType.LPStr)] string user_id,
            [In][Out] byte[] data, [MarshalAs(UnmanagedType.Bool)] Boolean encrypted, int type);
        [DllImport("MessengerBackend.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
        static private extern void call_send_message_seen(IntPtr pObject, [MarshalAs(UnmanagedType.LPStr)] string user_id,
            [MarshalAs(UnmanagedType.LPStr)] string message_id);
        [DllImport("MessengerBackend.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
        static private extern void call_request_active_users(IntPtr pObject, [MarshalAs(UnmanagedType.FunctionPtr)] RequestUsersCallBack pfResult);
        [DllImport("MessengerBackend.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
        static private extern IntPtr get_last_msg_id(IntPtr pObject);
        [DllImport("MessengerBackend.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
        static private extern int get_last_msg_time(IntPtr pObject);
        [DllImport("MessengerBackend.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
        static private extern IntPtr get_next_user(IntPtr pObject);
        [DllImport("MessengerBackend.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
        static private extern void free_user_list([MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] IntPtr[] data, int size);
        [DllImport("MessengerBackend.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
        static private extern void set_msg_status_changed_callback(IntPtr pObject, 
            [MarshalAs(UnmanagedType.FunctionPtr)] MessageStatusChangeCallback pfResult);
        [DllImport("MessengerBackend.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
        static private extern void set_msg_received_callback(IntPtr pObject, [MarshalAs(UnmanagedType.FunctionPtr)] MessageReceivedCallback pfResult);
        #endregion PInvokes
        #region Members
        private IntPtr m_native_object;
        RequestUsersCallBack m_request_users_callback;
        LoginRequestCallback m_login_request_callback;
        MessageStatusChangeCallback m_message_status_changed_callback;
        MessageReceivedCallback m_message_received_callback;
        #endregion Members
        public CBackendController(string server_url, ushort port,
            RequestUsersCallBack request_users_callback,
            LoginRequestCallback login_request_callback,
            MessageStatusChangeCallback msg_status_changed_callback, 
            MessageReceivedCallback msg_received_callback) {
            m_native_object = create_backend_instance(server_url, port);
            m_request_users_callback = request_users_callback;
            m_login_request_callback = login_request_callback;
            set_msg_status_changed_callback(m_native_object, msg_status_changed_callback);
            set_msg_received_callback(m_native_object, msg_received_callback);
        }
        public void Dispose() {
            Dispose(true);
        }
        protected virtual void Dispose(bool bDisposing) {
            if(m_native_object != IntPtr.Zero) {
                dispose_class(m_native_object);
                m_native_object = IntPtr.Zero;
            }
            if(bDisposing) {
                GC.SuppressFinalize(this);
            }
        }
        ~CBackendController() {
            Dispose(false);
        }
        #region Wrapper methods
        //public void SetMsgStatusChangedCallback(MessageStatusChangeCallback pfResult) {
        //    set_msg_status_changed_callback(m_native_object, pfResult);
        //}
        //public void SetMsgReceivedCallback(MessageReceivedCallback pfResult) {
        //    set_msg_received_callback(m_native_object, pfResult);
        //}
        public void Login(string user_id, string password, Boolean encrypted) {
            call_login(m_native_object, user_id, password, encrypted, m_login_request_callback);
        }
        public void Disconnect() {
            call_disconnect(m_native_object);
        }
        public void SendMessage(string user_id, ref byte[] data, Boolean encrypted, int type) {
            call_send_message(m_native_object, user_id, data, encrypted, type);
        }
        public void SendMessageSeen(string user_id, string message_id) {
            call_send_message_seen(m_native_object, user_id, message_id);
        }
        public void RequestActiveUsers() {
            call_request_active_users(m_native_object, m_request_users_callback);
        }
        public string GetLastMessageId() {
            IntPtr str_ptr = get_last_msg_id(m_native_object);
            return Marshal.PtrToStringUni(str_ptr);
        }
        public DateTime GetLastMessageDate() {
            return new DateTime(1970, 1, 1).ToLocalTime().AddSeconds(get_last_msg_time(m_native_object));
        }
        public List<string> GetUserList() {
            List<string> res = new List<string>();
            IntPtr user_name_ptr = get_next_user(m_native_object);
            while (user_name_ptr != IntPtr.Zero) {
                res.Add(Marshal.PtrToStringUni(user_name_ptr));
                user_name_ptr = get_next_user(m_native_object);
            }
            return res;
        }
        #endregion Wrapper methods
    }
}
