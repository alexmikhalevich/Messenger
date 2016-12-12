#include "include\messenger\callbacks.h"

namespace callbacks {
	typedef void(_cdecl *pManagedCallback) (int status);

	class CLoginCallback : public messenger::ILoginCallback {
	public:
		void OnOperationResult(messenger::operation_result::Type result) {

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