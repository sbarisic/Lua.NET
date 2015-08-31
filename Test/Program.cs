using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lua.NET;

namespace Test {
	class Program {
		static string ReadLine(string Prompt) {
			Console.Write(Prompt);
			return Console.ReadLine();
		}

		static void Main(string[] args) {
			Console.Title = "Test";

			string Str;
			while ((Str = ReadLine("> ")).Length > 0)
				try {

				} catch (Exception E) {
					Console.WriteLine(E.Message);
				}

			Console.WriteLine("Done!");
			Console.ReadLine();
		}
	}
}