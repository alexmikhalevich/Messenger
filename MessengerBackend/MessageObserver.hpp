#include "messenger\observers.h"

class CMessageObserver : public messenger::IMessagesObserver {
public:
	void OnMessageStatusChanged(const messenger::MessageId& msgId, messenger::message_status::Type status) {

	}

	void OnMessageReceived(const messenger::UserId& senderId, const messenger::Message& msg) {

	}
};