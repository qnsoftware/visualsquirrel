/* see LICENSE notice in solution root */

#pragma once

using namespace System;

namespace Squirrel {
namespace Squirrel3 {
	public ref class CompilerError
	{
	public:
		CompilerError(String^ error,int line,int col);
		int line;
		int column;
		String ^error;
	};
	public ref class Compiler
	{
		public:
			Compiler();
			bool Compile(String ^src,CompilerError^% error);
			~Compiler();
			!Compiler();
		public:
			String ^_lasterror_message;
			int _lasterror_line;
			int _lasterror_column;
		private:
			void ResetLastError();
			HSQUIRRELVM _vm;
	};
}
}
