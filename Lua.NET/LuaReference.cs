using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LuaNET {
	public class LuaReference : IDisposable {
		const int InvalidValue = -10;

		lua_StatePtr L;
		int Reference;

		public LuaReference(lua_StatePtr L, int Idx = -1) {
			this.L = L;

			if (Idx != -1) 
				Lua.lua_pushvalue(L, Idx);

			Reference = Lua.luaL_ref(L, Lua.LUA_REGISTRYINDEX);
		}

		public void GetRef() {
			EnsureReference();
			Lua.lua_rawgeti(L, Lua.LUA_REGISTRYINDEX, Reference);
		}

		void EnsureReference() {
			if (Reference == InvalidValue)
				throw new Exception("Trying to access a disposed reference");
		}

		public void Dispose() {
			EnsureReference();

			Lua.luaL_unref(L, Lua.LUA_REGISTRYINDEX, Reference);
			Reference = InvalidValue;
		}

		public string GetTypeName() {
			return Lua.lua_typename(L, GetLuaType());
		}

		public int GetLuaType() {
			GetRef();
			int Type = Lua.lua_type(L, -1);
			Lua.lua_pop(L, 1);
			return Type;
		}
	}

	public class LuaFuncRef : LuaReference {
		public LuaFuncRef(lua_StatePtr L, int Idx = -1) : base(L, Idx) {
		}
	}
}
