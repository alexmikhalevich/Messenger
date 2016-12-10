#pragma once
#include "messenger\messenger.h"
#include "Callbacks.hpp"
#include "MessageObserver.hpp"

class CMessengerBackend {
private:
	std::shared_ptr<messenger::IMessenger> m_messenger_instance;
	CLoginCallback* m_login_callback;
	CRequestUserCallback* m_request_user_callback;
	void _set_settings(const std::string& server_url, unsigned short port, messenger::MessengerSettings& msg_settings);
	void _set_policy(bool use_encryption, messenger::SecurityPolicy& sec_policy);
public:
	CMessengerBackend(const std::string& server_url, unsigned short port);
	~CMessengerBackend();
	void login(const std::string& user_id, const std::string& password, bool use_encryption);
	void disconnect();
	void send_message(const std::string& user_id, const messenger::MessageContent& content);
	void send_message_seen(const std::string& user_id, const std::string& message_id);
	void request_active_users();
	//void register_observer(CMessageObserver* observer);
	//void unregister_observer(CMessageObserver* observer);
};

