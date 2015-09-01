using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Linq.Expressions;
using System.Reflection;
using LuaNET;

namespace Test {
	class Program {
		static string ReadLine(string Prompt) {
			Console.Write(Prompt);
			return Console.ReadLine();
		}

		static int Wrapped(string S, double D) {
			Console.WriteLine("String: {0}", S);
			Console.WriteLine("Number: {0}", D);
			return 42;
		}

		static void Main(string[] args) {
			Console.Title = "Test";
			lua_StatePtr L = Lua.luaL_newstate();
			Lua.luaL_openlibs(L);

			Lua.lua_pushcfunction(L, Advanced.Wrap(new Func<object, object>((O) => {
				if (O != null)
					Console.WriteLine(O.GetType());
				else
					Console.WriteLine("Null");
				return O;
			})));
			Lua.lua_setglobal(L, "test");

			string Str;
			while ((Str = ReadLine("> ")).Length > 0)
				if (Lua.luaL_dostring(L, Str) != 0)
					Console.WriteLine(Lua.lua_tostring(L, -1));

			Console.WriteLine("Done!");
			Console.ReadLine();
		}
	}
}