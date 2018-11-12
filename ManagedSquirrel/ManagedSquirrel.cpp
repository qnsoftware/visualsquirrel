/* see LICENSE notice in solution root */

#include "stdafx.h"

#include "ManagedSquirrel.h"

namespace Squirrel {
	namespace Squirrel3 {

struct VMStub {
	gcroot<Compiler ^> comp;
};

//HSQUIRRELVM Compiler::_vm = NULL;
void compiler_error_handler(HSQUIRRELVM v,const SQChar * desc,const SQChar * source,SQInteger line,SQInteger column)
{
	SQUserPointer fp = sq_getforeignptr(v);
	if(fp) {
		VMStub *vs = (VMStub *)fp;
		Compiler ^cmp = vs->comp;
		cmp->_lasterror_message = gcnew String(desc);
		cmp->_lasterror_line = line;
		cmp->_lasterror_column = column;
		
	}
	else {
		Console::WriteLine("Foreign pointer is null");
	}
	Console::WriteLine(gcnew String(desc));
}


CompilerError::CompilerError(String^ error,int line,int col)
{
	this->error = error;
	this->line = line;
	this->column = col;
}

void Compiler::ResetLastError()
{
	_lasterror_message = nullptr;
	_lasterror_line = 0;
	_lasterror_column = 0;
}

Compiler::Compiler()
{
	_vm = sq_open(100);	
	
	sq_setcompilererrorhandler(_vm,compiler_error_handler);
}

Compiler::~Compiler()
{
	if(_vm) {
		sq_close(_vm);
	}
}

Compiler::!Compiler()
{
	this->~Compiler();
}

bool Compiler::Compile(String ^src,CompilerError^% error)
{
	error = nullptr;
	pin_ptr<const wchar_t> s = PtrToStringChars(src);
	VMStub ss;
	ss.comp = this;
	sq_setforeignptr(_vm,&ss);
	ResetLastError();
	//clears const table
	sq_pushconsttable(_vm);
	sq_clear(_vm, -1);
	sq_pop(_vm, 1);
	if(SQ_FAILED(sq_compilebuffer(_vm,s,src->Length,s,SQTrue))) {
		error = gcnew CompilerError(_lasterror_message,_lasterror_line,_lasterror_column);
		sq_setforeignptr(_vm,NULL);
		return false;
	}
	sq_setforeignptr(_vm,NULL);
	sq_pop(_vm,1);
	return true;
}
	}
}
