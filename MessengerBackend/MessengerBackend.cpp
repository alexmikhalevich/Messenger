#include "MessengerBackend.h"

CMessengerBackend::CMessengerBackend(const std::string& server_url, unsigned short port) {
	messenger::MessengerSettings msg_settings;
	CMessengerBackend::_set_settings(server_url, port, msg_settings);
	m_messenger_instance = messenger::GetMessengerInstance(msg_settings);
	m_login_callback = new CLoginCallback;
	m_request_user_callback = new CRequestUserCallback;
}

CMessengerBackend::~CMessengerBackend() {
	if (m_login_callback) delete m_login_callback;
	if (m_request_user_callback) delete m_request_user_callback;
}

void CMessengerBackend::_set_settings(const std::string& server_url, unsigned short port, messenger::MessengerSettings& msg_settings) {
	msg_settings.serverUrl = server_url;
	msg_settings.serverPort = port;
}

void CMessengerBackend::_set_policy(bool use_encryption, messenger::SecurityPolicy& sec_policy) {
	if (use_encryption) {
		sec_policy.encryptionAlgo = messenger::encryption_algorithm::RSA_1024;
		//TODO: set public key
	}
	else sec_policy.encryptionAlgo = messenger::encryption_algorithm::None;
}

void CMessengerBackend::login(const std::string& user_id, const std::string& password, bool use_encryption) {
	messenger::SecurityPolicy sec_policy;
	CMessengerBackend::_set_policy(use_encryption, sec_policy);
	m_messenger_instance->Login(user_id, password, sec_policy, m_login_callback);
}

void CMessengerBackend::disconnect() {
	m_messenger_instance->Disconnect();
}

void CMessengerBackend::send_message(const std::string& user_id, const messenger::MessageContent& content) {
	m_cur_message = m_messenger_instance->SendMessage(user_id, content);
}

void CMessengerBackend::send_message_seen(const std::string& user_id, const std::string& message_id) {
	m_messenger_instance->SendMessageSeen(user_id, message_id);
}

void CMessengerBackend::request_active_users() {
	m_messenger_instance->RequestActiveUsers(m_request_user_callback);
}

const char* CMessengerBackend::get_last_msg_id() {
	return m_cur_message.identifier.c_str();
}

std::time_t CMessengerBackend::get_last_msg_date() {
	return m_cur_message.time;
}

extern "C" __declspec(dllexport) CMessengerBackend* _cdecl create_backend_instance(char* server_url, unsigned short port) {
	return new CMessengerBackend(std::string(server_url), port);
}

extern "C" __declspec(dllexport) void _cdecl dispose_class(CMessengerBackend* pObject) {
	if (pObject != NULL) {
		delete pObject;
		pObject = NULL;
	}
}

extern "C" __declspec(dllexport) void _cdecl call_login(CMessengerBackend* pObject, char* user_id, char* password, bool use_encryption) {
	if (pObject != NULL) {
		std::string s_user_id(user_id);
		std::string s_password(password);
		pObject->login(s_user_id, s_password, use_encryption);
	}
}

extern "C" __declspec(dllexport) void _cdecl call_disconnect(CMessengerBackend* pObject) {
	if (pObject != NULL)
		pObject->disconnect();
}

/*
type = 1	text
type = 2	image
type = 3	video
*/
extern "C" __declspec(dllexport) void _cdecl call_send_message(CMessengerBackend* pObject, char* user_id, unsigned char* data, bool encrypted, int type) {
	messenger::MessageContent content;
	std::vector<unsigned char> v_data(data, data + sizeof(data) / sizeof(unsigned char));
	std::string s_user_id(user_id);
	content.data = v_data;
	content.encrypted = encrypted;
	switch (type) {
	case 1:
		content.type = messenger::message_content_type::Text;
		break;
	case 2:
		content.type = messenger::message_content_type::Image;
		break;
	case 3:
		content.type = messenger::message_content_type::Video;
		break;
	default:
		content.type = messenger::message_content_type::Text;
	}
	pObject->send_message(s_user_id, content);
}

extern "C" __declspec(dllexport) const char* _cdecl get_last_msg_id(CMessengerBackend* pObject) {
	const char* res = pObject->get_last_msg_id();
	return res;
}

extern "C" __declspec(dllexport) long int _cdecl get_last_msg_time(CMessengerBackend* pObject) {
	std::time_t res = pObject->get_last_msg_date();
	return static_cast<long int>(res);
}

extern "C" __declspec(dllexport) void _cdecl call_send_message_seen(CMessengerBackend* pObject, char* user_id, char* message_id) {
	std::string s_user_id(user_id);
	std::string s_message_id(message_id);
	pObject->send_message_seen(s_user_id, s_message_id);
}

extern "C" __declspec(dllexport) void _cdecl call_request_active_users(CMessengerBackend* pObject) {
	pObject->request_active_users();
}