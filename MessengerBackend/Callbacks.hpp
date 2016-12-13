#include "include\messenger\callbacks.h"
#include "messenger\messenger.h"
#include "MessageObserver.hpp"

namespace callbacks {
	typedef void(_stdcall *pManagedCallback) (int status);

	class CLoginCallback : public messenger::ILoginCallback {
	private:
		pManagedCallback m_callback;
		std::shared_ptr<messenger::IMessenger> m_msg_instance;
		CMessageObserver* m_msg_observer;
	public:
		CLoginCallback(std::shared_ptr<messenger::IMessenger> messenger_instance, CMessageObserver* message_observer) {
			m_msg_instance = messenger_instance;
			m_msg_observer = message_observer;
		}
		void set_callback(pManagedCallback callback_func) { m_callback = callback_func; }
		void OnOperationResult(messenger::operation_result::Type result) {
			if (result == messenger::operation_result::Type::Ok) m_msg_instance->RegisterObserver(m_msg_observer);
			m_callback(result);
		}
	};

	class CRequestUserCallback : public messenger::IRequestUsersCallback {
	private:
		messenger::UserList* m_online_users;
		pManagedCallback m_callback;
	public:
		CRequestUserCallback(messenger::UserList* list_ptr) :
			m_online_users(list_ptr) {}
		void set_callback(pManagedCallback callback_func) { m_callback = callback_func; }
		void OnOperationResult(messenger::operation_result::Type result, const messenger::UserList& users) {
			(*m_online_users) = users;
			m_callback(result);
		}
	};
}