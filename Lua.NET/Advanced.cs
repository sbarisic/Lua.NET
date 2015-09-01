using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Linq.Expressions;
using System.Reflection;

namespace LuaNET {
	public static class Advanced {
		static Dictionary<Type, Func<lua_StatePtr, int, object>> LuaToNetMarshals;
		static Dictionary<Type, Func<lua_StatePtr, object, int>> NetToLuaMarshals;

		static Advanced() {
			NetToLuaMarshals = new Dictionary<Type, Func<lua_StatePtr, object, int>>();
			LuaToNetMarshals = new Dictionary<Type, Func<lua_StatePtr, int, object>>();

			Advanced.AddTypeMarshal(typeof(object), (State, Idx) => {
				int T = Lua.lua_type(State, Idx);
				switch (T) {
					case Lua.LUA_TNONE:
						Lua.luaL_argerror(State, Idx, "expected a value");
						return null;
					case Lua.LUA_TNIL:
						return null;
					case Lua.LUA_TBOOLEAN:
						return Lua.lua_toboolean(State, Idx);
					case Lua.LUA_TNUMBER:
						return Lua.lua_tonumber(State, Idx);
					case Lua.LUA_TSTRING:
						return Lua.lua_tostring(State, Idx);
					case Lua.LUA_TTABLE:
					case Lua.LUA_TFUNCTION:
					case Lua.LUA_TTHREAD:
					case Lua.LUA_TLIGHTUSERDATA:
					case Lua.LUA_TUSERDATA:
					default:
						throw new NotImplementedException();
				}
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
		}

		static int Push(lua_StatePtr L, object Ret) {
			Type T = typeof(object);
			if (Ret != null)
				T = Ret.GetType();
			if (NetToLuaMarshals.ContainsKey(T))
				return NetToLuaMarshals[T](L, Ret);
			else
				throw new Exception("Unsupported Lua marshal type " + T);
		}

		static object Pop(lua_StatePtr L, int N, Type T) {
			if (LuaToNetMarshals.ContainsKey(T))
				return LuaToNetMarshals[T](L, N);
			else
				throw new Exception("Unsupported Lua marshal type " + T);
		}

		public static void AddTypeMarshal(Type T, Func<lua_StatePtr, int, object> LuaToNet,
			Func<lua_StatePtr, object, int> NetToLua) {
			LuaToNetMarshals.Add(T, LuaToNet);
			NetToLuaMarshals.Add(T, NetToLua);
		}

		public static MethodInfo GetMethodInfo(Expression<Action> A) {
			return (A.Body as MethodCallExpression).Method;
		}

		public static lua_CFunction Wrap(Delegate D) {
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
			return Expression.Lambda<lua_CFunction>(CFunc, L).Compile();
		}
	}
}