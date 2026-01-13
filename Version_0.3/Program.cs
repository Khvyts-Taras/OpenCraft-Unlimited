using System.IO;
using StbImageSharp;


namespace OpenCraft
{
	class Program
	{
		static void Main(string[] args)
		{
			using(Game game = new Game(1200, 800, 500))
			{
				game.Run();
			}
		}
	}
}