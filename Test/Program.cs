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
			Console.WriteLine("Running {0}", Lua.VERSION);
			lua_StatePtr L = Lua.luaL_newstate();
			Lua.luaL_openlibs(L);

			if (Lua.VERSION == LuaVersion.LuaJIT) { // LuaJIT print doesn't work :V
				Advanced.SetGlobal(L, "write", new Action<string>(Console.Write));
				Lua.luaL_dostring(L, "local _write = write write = nil function print(...) for _,v in pairs({...}) do _write(tostring(v)) _write('\\t') end _write('\\n') end");
			}
			Lua.luaL_dostring(L, "function printt(t) for k,v in pairs(t) do print(k, ' - ', v) end end");

			string Str;
			while ((Str = ReadLine("> ")).Length > 0)
				if (Lua.luaL_dostring(L, Str) != 0)
					Console.WriteLine(Lua.lua_tostring(L, -1));

			Console.WriteLine("Done!");
			Console.ReadLine();
		}
	}
}