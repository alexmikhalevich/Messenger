#include "messenger\observers.h"

typedef void(_stdcall *pMessageStatusChanged) (const char* msg_id, int str_len, int status);
typedef void(_stdcall *pMessageReceived) (const char* user_id, int user_id_len, const char* msg_id, int msg_len, long int time, 
										  int type, bool encrypted, unsigned char* data, int data_size);

class CMessageObserver : public messenger::IMessagesObserver {
private:
	pMessageStatusChanged m_status_changed_callback;
	pMessageReceived m_received_callback;
	messenger::UserId m_user;
public:
	void set_status_changed_callback(pMessageStatusChanged callback) { m_status_changed_callback = callback;  }
	void set_received_callback(pMessageReceived callback) { m_received_callback = callback; }
	void set_user(const messenger::UserId& user) { m_user = user; }
	void OnMessageStatusChanged(const messenger::MessageId& msgId, messenger::message_status::Type status) {
		const char* msg_id_ptr = msgId.c_str();
		m_status_changed_callback(msg_id_ptr, msgId.length(), status);
	}

	void OnMessageReceived(const messenger::UserId& senderId, const messenger::Message& msg) {
		if (senderId == m_user) return;
		unsigned char* data = new unsigned char[msg.content.data.size()];
		for (size_t i = 0; i < msg.content.data.size(); ++i)
			data[i] = msg.content.data[i];
		m_received_callback(senderId.c_str(), senderId.length(), msg.identifier.c_str(), msg.identifier.length(), static_cast<long int>(msg.time), 
							msg.content.type, msg.content.encrypted, data, msg.content.data.size());
	}
};