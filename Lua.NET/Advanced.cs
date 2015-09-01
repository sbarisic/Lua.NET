using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Linq.Expressions;
using System.Reflection;

namespace LuaNET {
	public static class Advanced {	

		static int Push(lua_StatePtr L, object Ret) {
			Type T = Ret.GetType();
			if (T == typeof(int)) {
				Lua.lua_pushinteger(L, (int)Ret);
				return 1;
			}
			return 0;
		}

		static object Pop(lua_StatePtr L, int N) {
			switch (Lua.lua_type(L, N)) {
				case Lua.LUA_TSTRING:
					return Lua.lua_tostring(L, N);
				case Lua.LUA_TNUMBER:
					return Lua.lua_tonumber(L, N);
			}
			return null;
		}

		static MethodInfo M(Expression<Action> A) {
			return (A.Body as MethodCallExpression).Method;
		}

		public static lua_CFunction Wrap(Delegate D) {
			ParameterExpression L = Expression.Parameter(typeof(lua_StatePtr), "L");
			MethodInfo PopMethod = M(() => Pop(default(lua_StatePtr), 0));
			MethodInfo PushMethod = M(() => Push(default(lua_StatePtr), null));

			List<Expression> LuaToCS = new List<Expression>();
			ParameterInfo[] Params = D.Method.GetParameters();
			for (int i = 0; i < Params.Length; i++) {
				LuaToCS.Add(Expression.Convert(Expression.Call(PopMethod, L, Expression.Constant(i + 1)), Params[i].ParameterType));
			}

			List<Expression> Body = new List<Expression>();
			MethodCallExpression Call = Expression.Call(D.Method, LuaToCS.ToArray());
			if (D.Method.ReturnType != typeof(void))
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