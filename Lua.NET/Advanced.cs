using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Linq.Expressions;

namespace LuaNET {
	public static class Advanced {

		public static lua_CFunction Wrap(Delegate D) {
			List<ParameterExpression> Params = new List<ParameterExpression>();
			Expression Body = null;

			return Expression.Lambda<lua_CFunction>(Body, Params).Compile();
		}

	}
}
