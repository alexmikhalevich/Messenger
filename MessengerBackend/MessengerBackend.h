#pragma once
#include "messenger\messenger.h"
#include "Callbacks.hpp"
#include "MessageObserver.hpp"

class CMessengerBackend {
private:
	std::shared_ptr<messenger::IMessenger> m_messenger_instance;
	callbacks::CLoginCallback* m_login_callback;
	callbacks::CRequestUserCallback* m_request_user_callback;
	messenger::Message m_cur_message;
	messenger::UserList m_online_users;
	CMessageObserver* m_message_observer;
	size_t m_cur_user;
	void _set_settings(const std::string& server_url, unsigned short port, messenger::MessengerSettings& msg_settings);
	void _set_policy(bool use_encryption, messenger::SecurityPolicy& sec_policy);
public:
	CMessengerBackend(const std::string& server_url, unsigned short port);
	~CMessengerBackend();
	void login(const std::string& user_id, const std::string& password, bool use_encryption, callbacks::pManagedCallback callback_func);
	void disconnect();
	void send_message(const std::string& user_id, const messenger::MessageContent& content);
	void send_message_seen(const std::string& user_id, const std::string& message_id);
	void request_active_users(callbacks::pManagedCallback callback_func);
	const char* get_next_user(int* str_len);
	const char* get_last_msg_id(int* str_len);
	std::time_t get_last_msg_date();
	void set_msg_status_changed_callback(pMessageStatusChanged callback_func);
	void set_msg_received_callback(pMessageReceived callback_func);
};

