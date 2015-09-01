using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Linq.Expressions;
using System.Reflection;

namespace LuaNET {
	public static class Advanced {
		static List<GCHandle> DelegateHandles;
		static Dictionary<Type, Func<lua_StatePtr, int, object>> LuaToNetMarshals;
		static Dictionary<Type, Func<lua_StatePtr, object, int>> NetToLuaMarshals;

		static Advanced() {
			DelegateHandles = new List<GCHandle>();
			NetToLuaMarshals = new Dictionary<Type, Func<lua_StatePtr, object, int>>();
			LuaToNetMarshals = new Dictionary<Type, Func<lua_StatePtr, int, object>>();

			Advanced.AddTypeMarshal(typeof(object), (State, Idx) => {
				int LT = Lua.lua_type(State, Idx);
				Type T = null;
				switch (LT) {
					case Lua.LUA_TNONE:
						Lua.luaL_argerror(State, Idx, "expected a value");
						return null;
					case Lua.LUA_TNIL:
						return null;
					case Lua.LUA_TBOOLEAN:
						T = typeof(bool);
						break;
					case Lua.LUA_TNUMBER:
						T = typeof(double);
						break;
					case Lua.LUA_TSTRING:
						T = typeof(string);
						break;
					case Lua.LUA_TLIGHTUSERDATA:
						T = typeof(IntPtr);
						break;
					case Lua.LUA_TFUNCTION:
						if (Lua.lua_iscfunction(State, Idx))
							T = typeof(lua_CFunction);
						else
							throw new NotImplementedException();
						break;
					case Lua.LUA_TTABLE:
					case Lua.LUA_TTHREAD:
					case Lua.LUA_TUSERDATA:
					default:
						throw new NotImplementedException();
				}

				return Pop(State, Idx, T);
			}, (State, Obj) => {
				if (Obj == null) {
					Lua.lua_pushnil(State);
					return 1;
				}
				throw new NotImplementedException();
			});

			Advanced.AddTypeMarshal(typeof(string), (State, Idx) => {
				return Lua.luaL_checkstring(State, Idx);
			}, (State, Obj) => {
				Lua.lua_pushstring(State, (string)Obj);
				return 1;
			});

			Advanced.AddTypeMarshal(typeof(double), (State, Idx) => {
				return Lua.luaL_checknumber(State, Idx);
			}, (State, Obj) => {
				Lua.lua_pushnumber(State, (double)Obj);
				return 1;
			});

			Advanced.AddTypeMarshal(typeof(float), (State, Idx) => {
				return (float)Lua.luaL_checknumber(State, Idx);
			}, (State, Obj) => {
				Lua.lua_pushnumber(State, (float)Obj);
				return 1;
			});

			Advanced.AddTypeMarshal(typeof(int), (State, Idx) => {
				return Lua.luaL_checkinteger(State, Idx);
			}, (State, Obj) => {
				Lua.lua_pushinteger(State, (int)Obj);
				return 1;
			});

			Advanced.AddTypeMarshal(typeof(long), (State, Idx) => {
				return (long)Lua.luaL_checkinteger(State, Idx);
			}, (State, Obj) => {
				Lua.lua_pushinteger(State, (int)(long)Obj);
				return 1;
			});

			Advanced.AddTypeMarshal(typeof(bool), (State, Idx) => {
				Lua.luaL_checktype(State, Idx, Lua.LUA_TBOOLEAN);
				return Lua.lua_toboolean(State, Idx);
			}, (State, Obj) => {
				Lua.lua_pushboolean(State, (bool)Obj);
				return 1;
			});

			Advanced.AddTypeMarshal(typeof(lua_CFunction), (State, Idx) => {
				Lua.luaL_checktype(State, Idx, Lua.LUA_TFUNCTION);
				return Lua.lua_tocfunction(State, Idx);
			}, (State, Obj) => {
				Lua.lua_pushcfunction(State, (lua_CFunction)Obj);
				return 1;
			});

			Advanced.AddTypeMarshal(typeof(Delegate), (State, Idx) => {
				throw new NotImplementedException();
			}, (State, Obj) => {
				Push(State, Wrap((Delegate)Obj));
				return 1;
			});

			Advanced.AddTypeMarshal(typeof(IntPtr), (State, Idx) => {
				Lua.luaL_checktype(State, Idx, Lua.LUA_TLIGHTUSERDATA);
				return Lua.lua_touserdata(State, Idx);
			}, (State, Obj) => {
				Lua.lua_pushlightuserdata(State, (IntPtr)Obj);
				return 1;
			});

			Advanced.AddTypeMarshal(typeof(lua_StatePtr), (State, Idx) => {
				Lua.luaL_checktype(State, Idx, Lua.LUA_TLIGHTUSERDATA);
				return new lua_StatePtr(Lua.lua_touserdata(State, Idx));
			}, (State, Obj) => {
				Lua.lua_pushlightuserdata(State, ((lua_StatePtr)Obj).StatePtr);
				return 1;
			});

			Advanced.AddTypeMarshal(typeof(Type), (State, Idx) => {
				Lua.luaL_checktype(State, Idx, Lua.LUA_TLIGHTUSERDATA);
				return GetTypeFromHandle(Lua.lua_touserdata(State, Idx));
			}, (State, Obj) => {
				Lua.lua_pushlightuserdata(State, GetHandleFromType((Type)Obj));
				return 1;
			});
		}

		static int Push(lua_StatePtr L, object Ret) {
			Type T = typeof(object);
			if (Ret != null)
				T = Ret.GetType();

			if (NetToLuaMarshals.ContainsKey(T))
				return NetToLuaMarshals[T](L, Ret);
			else
				foreach (var KV in NetToLuaMarshals)
					if (KV.Key.IsAssignableFrom(T))
						try {
							return KV.Value(L, Ret);
						} catch (NotImplementedException) {
							continue;
						}

			throw new Exception("Unsupported Lua marshal type " + T);
		}

		static object Pop(lua_StatePtr L, int N, Type T) {
			if (LuaToNetMarshals.ContainsKey(T))
				return LuaToNetMarshals[T](L, N);
			else
				foreach (var KV in LuaToNetMarshals)
					if (KV.Key.IsAssignableFrom(T))
						try {
							return KV.Value(L, N);
						} catch (NotImplementedException) {
							continue;
						}

			throw new Exception("Unsupported Lua marshal type " + T);
		}

		public static void AddTypeMarshal(Type T, Func<lua_StatePtr, int, object> LuaToNet,
			Func<lua_StatePtr, object, int> NetToLua) {
			if (LuaToNetMarshals.ContainsKey(T))
				throw new Exception("Marshal type " + T + " already registered");
			LuaToNetMarshals.Add(T, LuaToNet);
			NetToLuaMarshals.Add(T, NetToLua);
		}

		public static MethodInfo GetMethodInfo(Expression<Action> A) {
			return (A.Body as MethodCallExpression).Method;
		}

		public static Type GetTypeFromHandle(IntPtr H) {
			MethodInfo M = typeof(Type).GetMethod("GetTypeFromHandleUnsafe", BindingFlags.Static | BindingFlags.NonPublic);
			return (Type)M.Invoke(null, new object[] { H });
		}

		public static IntPtr GetHandleFromType(Type T) {
			return T.TypeHandle.Value;
		}

		public static lua_CFunction Wrap(Delegate D) {
			DelegateHandles.Add(GCHandle.Alloc(D));
			return Wrap(D.Method);
		}

		public static lua_CFunction Wrap(MethodInfo Method) {
			MethodInfo PushMethod = GetMethodInfo(() => Push(default(lua_StatePtr), null));
			MethodInfo PopMethod = GetMethodInfo(() => Pop(default(lua_StatePtr), 0, typeof(void)));
			List<Expression> LuaToCS = new List<Expression>();
			ParameterInfo[] Params = Method.GetParameters();

			ParameterExpression L = Expression.Parameter(typeof(lua_StatePtr), "L");

			for (int i = 0; i < Params.Length; i++) {
				MethodCallExpression PopMethodCall = Expression.Call(PopMethod, L, Expression.Constant(i + 1),
					Expression.Constant(Params[i].ParameterType, typeof(Type)));
				LuaToCS.Add(Expression.Convert(PopMethodCall, Params[i].ParameterType));
			}

			List<Expression> Body = new List<Expression>();
			MethodCallExpression Call = Expression.Call(Method, LuaToCS.ToArray());
			if (Method.ReturnType != typeof(void))
				Body.Add(Expression.Call(PushMethod, L, Expression.Convert(Call, typeof(object))));
			else {
				Body.Add(Call);
				Body.Add(Expression.Constant(0));
			}

			Expression CFunc = Body[0];
			if (Body.Count > 1)
				CFunc = Expression.Block(Body.ToArray());
			lua_CFunction Func = Expression.Lambda<lua_CFunction>(CFunc, L).Compile();
			DelegateHandles.Add(GCHandle.Alloc(Func));
			return Func;
		}

		public static void SetGlobal(lua_StatePtr L, string Name, object Obj) {
			Push(L, Obj);
			Lua.lua_setglobal(L, Name);
		}

		public static void OpenLib(lua_StatePtr L, Type StaticType, int NUp = 0, bool SkipInvalid = true) {
			if (StaticType.IsClass && StaticType.IsAbstract && StaticType.IsSealed) {
				List<luaL_Reg> Regs = new List<luaL_Reg>();
				MethodInfo[] Methods = StaticType.GetMethods(BindingFlags.Static | BindingFlags.Public);
				if (Methods.Length == 0)
					return;
				for (int i = 0; i < Methods.Length; i++) {
					try {
						Regs.Add(new luaL_Reg(Methods[i].Name, Wrap(Methods[i])));
					} catch (Exception) {
						continue;
					}
				}
				Regs.Add(luaL_Reg.NULL);
				Lua.luaL_openlib(L, StaticType.Name, Regs.ToArray(), NUp);
			} else
				throw new Exception("Cannot register non static class as library");
		}
	}
}