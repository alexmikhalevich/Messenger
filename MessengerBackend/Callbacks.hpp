#include "include\messenger\callbacks.h"

class CLoginCallback : public messenger::ILoginCallback {
public:
	void OnOperationResult(messenger::operation_result::Type result) {

	}
};

class CRequestUserCallback : public messenger::IRequestUsersCallback {
public:
	void OnOperationResult(messenger::operation_result::Type result, const messenger::UserList& users) {

	}
};