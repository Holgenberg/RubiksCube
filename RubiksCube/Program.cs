// See https://aka.ms/new-console-template for more information
using System.Diagnostics;
using System.Runtime.InteropServices;


MoveHistory history = RubikPattern.GetPattern(0);
while (true)
{
	Console.WriteLine("Type a command: help, [LRFBUD][2'], [xyz], Exit, reset, undo, history, list, apply, read, or [0-{0}]", RubikPattern.patterns.Count - 1);
	string input = Console.ReadLine().ToUpper().Trim();
	if (String.IsNullOrEmpty(input))
	{
		history.PrintLastState();
	}
	else if (input == "EXIT")
		return;
	else if (input == "HELP")
	{
		string helpText = String.Format("Help: this command.\n" +
			"Exit: exit this program.\n" +
			"Reset: reset the cube to start position.\n" +
			"Undo: undo the last move.\n" +
			"History: moves you have made so far.\n" +
			"List: list the {0} cube patterns for start position.\n" +
			"<n>: set the start position to the <n>th (n=0..{0}) pattern in the list.\n" +
			"Read: read the start position from console.\n" +
			"Apply [LRFBUD][2']] [ [LRFBUD][2']]*: apply a sequence of moves, separated with space.\n" +
			"   Example: Apply U D\n" +
			"x: (rotate): rotate the Cube up.\n" +
			"y: (rotate): rotate the Cube to the counter-clockwise.\n" +
			"z: (rotate): rotate the Cube clockwise.\n" +
			"[LRFBUD][2']: a move for the cube:\n", RubikPattern.patterns.Count - 1);

		foreach (string dir in new string[] { "Left", "Right", "Front", "Back", "Up", "Down" })
		{
			helpText = helpText + String.Format("\t{0},{0}2,{0}': move {1} face for 90, 180, 270 degree clockwise. \n", dir[0], dir);
		}
		Console.WriteLine(helpText);
	}
	else if (input == "RESET")
	{
		history = new MoveHistory(history.GetStartState());
		history.PrintLastState();
	}
	else if (input == "HISTORY")
	{
		history.PrintHistory();
	}
	else if (input == "UNDO")
	{
		history.Undo();
		history.PrintLastState();
	}
	else if (input == "LIST")
	{
		for (int i = 0; i < RubikPattern.patterns.Count; i++)
		{
			Console.WriteLine("{0}:\t{1}", i, RubikPattern.patterns[i].description);
		}
	}
	else if (Char.IsDigit(input, 0))
	{
		try
		{
			int index = Convert.ToInt32(input);
			history = RubikPattern.GetPattern(index);
		}
		catch (Exception e)
		{
			Console.WriteLine(e);
		}
	}
	else if (input.StartsWith("READ"))
	{
		try
		{
			CubeReader reader = new CubeReader();
			Cube s = reader.ReadCube();
			if (s != null)
			{
				history = new MoveHistory(s);
			}
		}
		catch (Exception e)
		{
			Console.WriteLine(e);
		}
		history.PrintLastState();
	}
	else if (input.StartsWith("APPLY"))
	{
		string[] moves = input.Substring("APPLY".Length).Split(' ');

		bool ok = true;
		int moveCount = 0;
		foreach (string str in moves)
		{
			if (!String.IsNullOrEmpty(str) && !Move.IsValidMove(str))
			{
				Console.WriteLine("Please remove the invalid move [{0}] from input {1}", str, input);
				ok = false;
			}
			else if (Move.IsValidMove(str))
			{
				moveCount++;
			}
		}

		if (moveCount == 0)
		{
			Console.WriteLine("please specify a sequence of moves");
		}
		else if (ok)
		{
			Cube s = Move.ApplyActions(history.GetCurrent().Clone(), moves);
			history.AddToHistoryIfNotDuplicate(s);
		}
	}
	else if (!Move.IsValidMove(input))
	{
		using (new ConsoleColor((int)ConsoleColor.ForeGroundColor.Red | (int)ConsoleColor.ForeGroundColor.White))
		{
			Console.WriteLine("invalid input!");
		}
	}
	else
	{
		Move m = Move.GetMove(input);
		Cube state = m.ActOn(history.GetCurrent());
		history.AddToHistoryIfNotDuplicate(state);
	}
}        

public class ConsoleColor : IDisposable
{
	private const int STD_OUTPUT_HANDLE = -11;
	private static readonly IntPtr hConsole = GetStdHandle(STD_OUTPUT_HANDLE);

	[StructLayout(LayoutKind.Sequential)]
	struct CONSOLE_SCREEN_BUFFER_INFO
	{
		uint dwSize;
		uint dwCursorPosition;
		public int wAttributes;
		ulong srWindow;
		uint dwMaximumWindowSize;
	};

	[DllImportAttribute("Kernel32.dll")]
	private static extern bool GetConsoleScreenBufferInfo(
		IntPtr hConsoleOutput,
		out CONSOLE_SCREEN_BUFFER_INFO consoleScreenBufferInfo
		);

	// input, output, or error device
	[DllImportAttribute("Kernel32.dll")]
	private static extern IntPtr GetStdHandle(int nStdHandle);

	[DllImportAttribute("Kernel32.dll")]
	private static extern bool SetConsoleTextAttribute(
		IntPtr hConsoleOutput, // handle to screen buffer
		int wAttributes    // text and background colors
		);

	public enum ForeGroundColor : int
	{
		Black = 0x0000,
		Blue = 0x0001,
		Green = 0x0002,
		Cyan = 0x0003,
		Red = 0x0004,
		Magenta = 0x0005,
		Yellow = 0x0006,
		Grey = 0x0007,
		White = 0x0008
	}

	public enum BackGroundColor : int
	{
		Black = 0x0000,
		Blue = 0x00010,
		Green = 0x00020,
		Cyan = 0x00030,
		Red = 0x00040,
		Magenta = 0x00050,
		Yellow = 0x00060,
		Grey = 0x00070,
		White = 0x00080
	}

	private int oldTextAttributes;

	public ConsoleColor(int color)
	{
		CONSOLE_SCREEN_BUFFER_INFO consoleScreenBufferInfo;
		GetConsoleScreenBufferInfo(hConsole, out consoleScreenBufferInfo);
		this.oldTextAttributes = consoleScreenBufferInfo.wAttributes;
		int wTextAttributes = (this.oldTextAttributes & ~0xFF) | color;
		SetConsoleTextAttribute(hConsole, wTextAttributes);
	}

	public void Dispose()
	{
		SetConsoleTextAttribute(hConsole, oldTextAttributes);
	}
}

//specify the surrounding neighbors of each Face
//this is the description of 2D layout of a Cube
class FaceLayout2D
{
	public sbyte self, right, up, left, down;
	public FaceLayout2D(sbyte s, sbyte r, sbyte u, sbyte l, sbyte d)
	{
		self = s;
		right = r;
		up = u;
		left = l;
		down = d;
	}

	public static Dictionary<sbyte, FaceLayout2D> layout = new Dictionary<sbyte, FaceLayout2D>();
	static FaceLayout2D()
	{
		layout.Add(Face.Up, new FaceLayout2D(Face.Up, Face.Right, Face.Back, Face.Left, Face.Front));
		layout.Add(Face.Left, new FaceLayout2D(Face.Left, Face.Front, Face.Up, Face.Back, Face.Down));
		layout.Add(Face.Front, new FaceLayout2D(Face.Front, Face.Right, Face.Up, Face.Left, Face.Down));
		layout.Add(Face.Right, new FaceLayout2D(Face.Right, Face.Back, Face.Up, Face.Front, Face.Down));
		layout.Add(Face.Down, new FaceLayout2D(Face.Down, Face.Right, Face.Front, Face.Left, Face.Back));
		layout.Add(Face.Back, new FaceLayout2D(Face.Back, Face.Right, Face.Down, Face.Left, Face.Up));
	}
}

class FaceLayout3D
{
	public static FaceLayout3D[] face3DLayouts;
	static FaceLayout3D()
	{
		/* A cube has six faces, lets name them 0-5
		 *   0 F = front face
		 *   1 R = right face
		 *   2 U = up face
		 *   3 L = left face
		 *   4 D = down face
		 *   5 B = back face
		 *                ___
		 *               /2 /|
		 *              /__/ | 5
		 *             |   |1|   
		 *           3 | 0 | / 
		 *             |___|/
		 *               4  
		 * If we open them into 2 dimensions:
		 *          ___
		 *         |   |
		 *         | 2 |
		 *      ___|___|___
		 *     |   |   |   |
		 *     | 3 | 0 | 1 |
		 *     |___|___|___|
		 *         |   |
		 *         | 4 |
		 *         |___|
		 *         |   |        
		 *         | 5 |
		 *         |___|
		 * 
		 * If we change the front side and right side, we will get these results:
		 *                  
		 *        ___              ___             ___      
		 *       |   |            |   |           |   |
		 *       | 0 |            | 0 |           | 0 |
		 *    ___|___|___      ___|___|___     ___|___|___
		 *   |   |   |   |    |   |   |   |   |   |   |   |
		 *   | 4 | 1 | 2 |    | 1 | 2 | 3 |   | 2 | 3 | 4 |
		 *   |___|___|___|    |___|___|___|   |___|___|___|
		 *       |   |            |   |           |   |
		 *       | 5 |            | 5 |           | 5 |
		 *       |___|            |___|           |___|
		 *       |   |            |   |           |   |       
		 *       | 3 |            | 4 |           | 1 |
		 *       |___|            |___|           |___|
		 * 
		 *        ___                  ___
		 *       |   |                |   |
		 *       | 0 |                | 4 |
		 *    ___|___|___          ___|___|___
		 *   |   |   |   |        |   |   |   |
		 *   | 3 | 4 | 1 |        | 3 | 5 | 1 |
		 *   |___|___|___|        |___|___|___|
		 *       |   |                |   |
		 *       | 5 |                | 2 |
		 *       |___|                |___|
		 *       |   |                |   |      
		 *       | 2 |                | 0 |
		 *       |___|                |___|
		 * 
		 */
		//we'll record the above result in a mattrix:
		face3DLayouts = new FaceLayout3D[6];
		face3DLayouts[0] = new FaceLayout3D(0, 1, 2, 3, 4, 5);
		face3DLayouts[1] = new FaceLayout3D(1, 2, 0, 4, 5, 3);
		face3DLayouts[2] = new FaceLayout3D(2, 3, 0, 1, 5, 4);
		face3DLayouts[3] = new FaceLayout3D(3, 4, 0, 2, 5, 1);
		face3DLayouts[4] = new FaceLayout3D(4, 1, 0, 3, 5, 2);
		face3DLayouts[5] = new FaceLayout3D(5, 1, 4, 3, 2, 0);
		//Note, the above matrix is not symetric. 
		//If we number the faces in a symetic way, the matrix would look more regular: 0->0, 1->1, 2->2, 3->4, 4->5, 5>3
	}
	sbyte[] order;
	sbyte[] reverseOrder;
	private FaceLayout3D(sbyte a1, sbyte a2, sbyte a3, sbyte a4, sbyte a5, sbyte a6)
	{
		order = new sbyte[6] { a1, a2, a3, a4, a5, a6 };
		reverseOrder = new sbyte[6];
		for (sbyte i = 0; i < 6; i++)
		{
			for (sbyte j = 0; j < 6; j++)
			{
				if (order[j] == i)
				{
					reverseOrder[i] = j;
				}
			}
		}
	}

	public sbyte this[int index]
	{
		get
		{
			return order[index];
		}
	}

	public static sbyte MapFrontFrom(sbyte side, sbyte facelet)
	{
		FaceLayout3D r = face3DLayouts[side];
		// order[i] - > i
		return r.reverseOrder[facelet];
	}

	public static sbyte MapFrontTo(sbyte side, sbyte facelet)
	{
		FaceLayout3D r = face3DLayouts[side];
		// i - > order[i]
		return r.order[facelet];
	}

}

class Cube
{
	Face[] faces;
	public int step;
	public string path;
	private Cube()
	{
		step = 0;
		faces = new Face[6];
	}

	public Cube ApplyActions(string[] actions)
	{
		Move.ApplyActions(this, actions);
		return this;
	}

	public void Print()
	{
		System.Console.WriteLine("\n\nstep = {0}, path={1}", step, path);
		//self, right, up, left, down
		Print3Faces(null, FaceLayout2D.layout[Face.Up], null);
		Print3Faces(FaceLayout2D.layout[Face.Left],
			FaceLayout2D.layout[Face.Front],
			FaceLayout2D.layout[Face.Right]
			);
		Print3Faces(null, FaceLayout2D.layout[Face.Down], null);
		Print3Faces(null, FaceLayout2D.layout[Face.Back], null);
	}

	private void Print3Faces(FaceLayout2D left, FaceLayout2D center, FaceLayout2D right)
	{
		string[] s1 = GetFaceString(left);
		string[] s2 = GetFaceString(center);
		string[] s3 = GetFaceString(right);

		for (int i = 0; i < 3; i++)
		{
			//Console.WriteLine("{0}|{1}|{2}", s1[i], s2[i], s3[i]);
			bool first = true;
			foreach (string[] s in new string[][] { s1, s2, s3 })
			{
				if (!first)
				{
					Console.Write('|');
				}
				first = false;
				string[] cells = s[i].Split(':');
				if (cells.Length == 1)
				{
					Console.Write("{0}", s[i]);
				}
				else
				{
					foreach (string c in cells)
					{
						if (c.Trim().Length > 0)
						{
							int color = c.Trim()[0] - '0';
							//map the color to what is on my cube
							color = (int)Face.GetColorForFace((sbyte)color);
							//set background color with intensity
							using (new ConsoleColor(color | (int)ConsoleColor.BackGroundColor.White))
							{
								Console.Write("{0}", c);
							}
							Console.Write(":");
						}
					}
				}
			}
			Console.WriteLine();
		}
		Console.WriteLine("____________________________________");
	}

	private string[] GetFaceString(FaceLayout2D face)
	{
		if (face == null)
		{
			return new string[3] { "            ", "            ", "            " };
		}

		return faces[face.self].GetFaceString(face);
	}

	public Cube Clone()
	{
		Cube ret = new Cube();
		ret.step = step;
		ret.path = path;
		for (int i = 0; i < faces.Length; i++)
		{
			ret.faces[i] = faces[i].Clone();
		}

		return ret;
	}

	static public Cube GetCubeFromReader(CubeReader reader)
	{
		Cube ret = GetInitState();
		ret.ResetFromReader(reader);
		return ret;
	}
	private void ResetFromReader(CubeReader reader)
	{
		for (sbyte side = 0; side < 6; side++)
		{
			Face face = faces[side];
			face.ResetFromCubeReader(reader, FaceLayout2D.layout[side], side);
		}
		//adjust the neighbor's color
		for (sbyte side = 0; side < 6; side++)
		{
			Face face = faces[side];
			foreach (Cubie c in face.cubies)
			{
				CubiePosition p = c.position;
				List<sbyte> list = new List<sbyte>();
				list.Add(p.a1);
				list.Add(p.a2);
				list.Add(p.a3);
				list.Remove(side);
				sbyte a2 = list[0];
				sbyte a3 = list[1];
				c.color = new CubieColor(
					faces[side][p.a1, p.a2, p.a3].color,
					faces[a2][p.a1, p.a2, p.a3].color,
					faces[a3][p.a1, p.a2, p.a3].color
					);
			}
		}
	}

	public int CompareTo(Cube other)
	{
		int ret = 0;
		for (int i = 0; i < 6; i++)
		{
			ret = faces[i].CompareTo(other.faces[i]);
			if (ret != 0)
			{
				return ret;
			}
		}

		return 0;
	}

	//return two Cubes that are similar
	public bool Similar(Cube other)
	{
		Cube normalizeMe = Clone();
		normalizeMe.Normalize();
		Cube normalizeOther = other.Clone();
		normalizeOther.Normalize();
		return normalizeMe.CompareTo(normalizeOther) == 0;
	}

	private void Normalize()
	{
		sbyte FaceIndexWithColor0 = GetFaceIndexForColor(Face.Front);
		//Make 0 as the color which is in faces[0]

		MapFrontFrom(FaceIndexWithColor0);
		RotateRightFaceToRight();
		foreach (Face f in faces)
		{
			f.Normalize();
		}
	}

	private void RotateRightFaceToRight()
	{
		sbyte FaceIndexWithColor1 = GetFaceIndexForColor(Face.Right);
		//Make 1 as the color which is in faces[1]
		if (FaceIndexWithColor1 == Face.Right)
		{
			return;
		}

		//Rotate the cube so that the color 'Face.Right' center is facing right
		foreach (Face face in faces)
		{
			for (sbyte side = FaceIndexWithColor1; side != Face.Right; side = Face.RotateFrontClockwise90Degree(side))
			{
				face.RotateFrontClockwise90Degree();
			}
		}

		AdjustFaces();
	}

	private sbyte GetFaceIndexForColor(sbyte color)
	{
		sbyte i = 0;
		for (i = 0; i < 6; i++)
		{
			if (faces[i].Center(false) == color)
			{
				return i;
			}
		}

		throw new ApplicationException(String.Format("color {0} is not found", color));
	}

	public void RotateFrontClockwise90Degree()
	{
		//rotate the front side
		faces[Face.Front].RotateFrontClockwise90Degree();
		//For the right, up, left, and down side, we need to rotate one row that is close to the front side
		//we start at the up side. The three cubies are the [up, front, {left/front/right}]
		foreach (sbyte side in new sbyte[] { Face.Left, Face.Front, Face.Right })
		{
			sbyte currentFace = Face.Up;
			sbyte currentPosition = side;
			CubieColor saved = faces[currentFace][currentFace, Face.Front, currentPosition];
			for (int i = 0; i < 4; i++)
			{
				// go counter-clockwise to update each value
				sbyte nextFace = Face.RotateFrontCounterClockwise90Degree(currentFace);
				sbyte nextPosition = Face.RotateFrontCounterClockwise90Degree(currentPosition);
				if (i == 3)
				{
					faces[currentFace][currentFace, Face.Front, currentPosition] = saved;
				}
				else
				{
					faces[currentFace][currentFace, Face.Front, currentPosition] = faces[nextFace][nextFace, Face.Front, nextPosition];
				}
				currentFace = nextFace;
				currentPosition = nextPosition;
			}
		}
	}

	public void RotateFrontMiddleLayerClockwise90Degree()
	{
		//For the right, up, left, and down side, we need to rotate one row that is close to the front side
		//we start at the up side. The three cubies are the [up, front, {left/front/right}]
		foreach (sbyte side in new sbyte[] { Face.Left, Face.Up, Face.Right })
		{
			sbyte currentFace = Face.Up;
			sbyte currentPosition = side;
			CubieColor saved = faces[currentFace][currentFace, currentFace, currentPosition];
			for (int i = 0; i < 4; i++)
			{
				// go counter-clockwise to update each position
				sbyte nextFace = Face.RotateFrontCounterClockwise90Degree(currentFace);
				sbyte nextPosition = Face.RotateFrontCounterClockwise90Degree(currentPosition);
				if (i == 3)
				{
					faces[currentFace][currentFace, currentFace, currentPosition] = saved;
				}
				else
				{
					faces[currentFace][currentFace, currentFace, currentPosition] = faces[nextFace][nextFace, nextFace, nextPosition];
				}
				currentFace = nextFace;
				currentPosition = nextPosition;
			}
		}

		AdjustFaces();
	}

	public void MapFrontFrom(sbyte side)
	{
		Debug.Assert(side < 6);
		foreach (Face e in faces)
		{
			e.MapFrontFrom(side);
		}

		AdjustFaces();
	}

	public void MapFrontTo(sbyte side)
	{
		Debug.Assert(side < 6);
		foreach (Face e in faces)
		{
			e.MapFrontTo(side);
		}
		AdjustFaces();
	}

	private void AdjustFaces()
	{
		//faces[i].centercolor = i
		Face[] backup = (Face[])faces.Clone();
		foreach (Face f in backup)
		{
			faces[f.Center(true)] = f;
		}
	}
	public static Cube GetInitState()
	{
		Cube ret = new Cube();
		for (sbyte i = 0; i < 6; i++)
		{
			FaceLayout3D r = FaceLayout3D.face3DLayouts[i];
			ret.faces[i] = new Face(i, r[1], r[2], r[3], r[4]);
		}
		return ret;
	}
}
class Move
{
	/* The moves are learned from
	 * http://peter.stillhq.com/jasmine/rubikscubesolution.html
	 * 
	 * F,F2,F' means clockwise rotate Front edge 90*1,2,3 degree.
	 * 
	 * */

	public string name;

	delegate void MoveFunction(Cube s);
	MoveFunction action;

	public Cube ActOn(Cube s)
	{
		Cube ret = s.Clone();
		action(ret);
		ret.step++;
		ret.path += name;
		return ret;
	}

	public static Cube ApplyActions(Cube s, string[] actions)
	{
		s.Print();
		foreach (string a in actions)
		{
			if (!String.IsNullOrEmpty(a))
			{
				allMoves[a].action(s);
				s.step++;
				s.path += a;
				s.Print();
			}
		}
		return s;
	}

	private static Dictionary<string, Move> allMoves;
	public static Move GetMove(string move)
	{
		return allMoves[move];
	}
	public static bool IsValidMove(string move)
	{
		return allMoves.ContainsKey(move);
	}

	static void AddMove(string name, MoveFunction action)
	{
		allMoves.Add(name, new Move(name, action));
	}

	/**
	 * http://en.wikipedia.org/wiki/Rubik's_Cube#Move_notation
	 * F (Front): the side currently facing you 
	 * B (Back): the side opposite the front 
	 * U (Up): the side above or on top of the front side 
	 * D (Down): the side opposite the top, underneath the Cube 
	 * L (Left): the side directly to the left of the front 
	 * R (Right): the side directly to the right of the front 
	 * f (Front two layers): the side facing you and the corresponding middle layer 
	 * b (Back two layers): the side opposite the front and the corresponding middle layer 
	 * u (Up two layers) : the top side and the corresponding middle layer 
	 * d (Down two layers) : the bottom layer and the corresponding middle layer 
	 * l (Left two layers) : the side to the left of the front and the corresponding middle layer 
	 * r (Right two layers) : the side to the right of the front and the corresponding middle layer 
	 * x (rotate): rotate the Cube up 
	 * y (rotate): rotate the Cube to the counter-clockwise 
	 * z (rotate): rotate the Cube clockwise 
	 * */
	static Move()
	{
		allMoves = new Dictionary<string, Move>();
		AddMove("F", s => RotateFront(s, 1));
		AddMove("F2", s => RotateFront(s, 2));
		AddMove("F'", s => RotateFront(s, 3));
		AddMove("L", s => RotateFace(s, 1, Face.Left));
		AddMove("L2", s => RotateFace(s, 2, Face.Left));
		AddMove("L'", s => RotateFace(s, 3, Face.Left));
		AddMove("U", s => RotateFace(s, 1, Face.Up));
		AddMove("U2", s => RotateFace(s, 2, Face.Up));
		AddMove("U'", s => RotateFace(s, 3, Face.Up));
		AddMove("R", s => RotateFace(s, 1, Face.Right));
		AddMove("R2", s => RotateFace(s, 2, Face.Right));
		AddMove("R'", s => RotateFace(s, 3, Face.Right));
		AddMove("D", s => RotateFace(s, 1, Face.Down));
		AddMove("D2", s => RotateFace(s, 2, Face.Down));
		AddMove("D'", s => RotateFace(s, 3, Face.Down));
		AddMove("B", s => RotateFace(s, 1, Face.Back));
		AddMove("B2", s => RotateFace(s, 2, Face.Back));
		AddMove("B'", s => RotateFace(s, 3, Face.Back));

		AddMove("f", s => { RotateFront(s, 1); RotateFrontMiddleLayer(s, 1); });
		AddMove("f2", s => { RotateFront(s, 2); RotateFrontMiddleLayer(s, 2); });
		AddMove("f'", s => { RotateFront(s, 3); RotateFrontMiddleLayer(s, 3); });
		AddMove("l", s => RotateFaceAndMiddleLayer(s, 1, Face.Left));
		AddMove("l2", s => RotateFaceAndMiddleLayer(s, 2, Face.Left));
		AddMove("l'", s => RotateFaceAndMiddleLayer(s, 3, Face.Left));
		AddMove("u", s => RotateFaceAndMiddleLayer(s, 1, Face.Up));
		AddMove("u2", s => RotateFaceAndMiddleLayer(s, 2, Face.Up));
		AddMove("u'", s => RotateFaceAndMiddleLayer(s, 3, Face.Up));
		AddMove("r", s => RotateFaceAndMiddleLayer(s, 1, Face.Right));
		AddMove("r2", s => RotateFaceAndMiddleLayer(s, 2, Face.Right));
		AddMove("r'", s => RotateFaceAndMiddleLayer(s, 3, Face.Right));
		AddMove("d", s => RotateFaceAndMiddleLayer(s, 1, Face.Down));
		AddMove("d2", s => RotateFaceAndMiddleLayer(s, 2, Face.Down));
		AddMove("d'", s => RotateFaceAndMiddleLayer(s, 3, Face.Down));
		AddMove("b", s => RotateFaceAndMiddleLayer(s, 1, Face.Back));
		AddMove("b2", s => RotateFaceAndMiddleLayer(s, 2, Face.Back));
		AddMove("b'", s => RotateFaceAndMiddleLayer(s, 3, Face.Back));

		//x (rotate): rotate the Cube up 
		//Front->Up, Right side the same. 
		//So, it is combination of L' and r
		AddMove("X", s => { RotateFace(s, 3, Face.Left); RotateFaceAndMiddleLayer(s, 1, Face.Right); });
		//y (rotate): rotate the Cube to the counter-clockwise 
		//It is combaintion of U' and d
		AddMove("Y", s => { RotateFace(s, 3, Face.Up); RotateFaceAndMiddleLayer(s, 1, Face.Down); });
		//z (rotate): rotate the Cube clockwise 
		//It is the combination of U and d'
		AddMove("Z", s => { RotateFace(s, 1, Face.Up); RotateFaceAndMiddleLayer(s, 3, Face.Down); });
	}

	private Move(string name, MoveFunction action)
	{
		this.name = name;
		this.action = action;
	}

	//clockwise rotate Front face 90*n degree
	private static void RotateFront(Cube s, int n)
	{
		Debug.Assert(n < 4);
		for (int i = 0; i < n; i++)
		{
			s.RotateFrontClockwise90Degree();
		}
	}

	//clockwise rotate specified face 90 degree
	private static void RotateFace(Cube s, int n, sbyte face)
	{
		Debug.Assert(n < 4);
		Debug.Assert(face < 6);
		s.MapFrontFrom(face);
		RotateFront(s, n);
		s.MapFrontTo(face);
	}

	private static void RotateFrontMiddleLayer(Cube s, int n)
	{
		Debug.Assert(n < 4);
		for (int i = 0; i < n; i++)
		{
			s.RotateFrontMiddleLayerClockwise90Degree();
		}
	}

	//clockwise rotate specified face 90 degree
	private static void RotateFaceMiddleLayer(Cube s, int n, sbyte face)
	{
		Debug.Assert(n < 4);
		Debug.Assert(face < 6);
		s.MapFrontFrom(face);
		RotateFrontMiddleLayer(s, n);
		s.MapFrontTo(face);
	}

	private static void RotateFaceAndMiddleLayer(Cube s, int n, sbyte face)
	{
		RotateFace(s, n, face);
		RotateFaceMiddleLayer(s, n, face);
	}
}
class CubiePosition
{
	/*
	 * For position:
	 *  1. when a1=a2=a3, it is a center cubie
	 *  2. when a1==a2 < a3, it is a edge cubie
	 *  3. when a1<a3<a3, it is is a corner cubie
	 *  */
	public sbyte a1, a2, a3;

	public CubiePosition(sbyte b1, sbyte b2, sbyte b3)
	{
		a1 = b1;
		a2 = b2;
		a3 = b3;
		AdjustOrder();
	}

	private void AdjustOrder()
	{
		//a1<=a3<=a3
		sbyte b1 = a1;
		sbyte b2 = a2;
		sbyte b3 = a3;
		a1 = Math.Min(b1, Math.Min(b2, b3));
		a3 = Math.Max(b1, Math.Max(b2, b3));
		a2 = (sbyte)(b1 + b2 + b3 - a1 - a3); //middle
		if (a2 == a3)
		{
			//only keep the smaller
			a2 = a1;
		}
	}
	public override string ToString()
	{
		return String.Format("({0},{1},{2})", a1, a2, a3);
	}
	public CubiePosition Clone()
	{
		return new CubiePosition(a1, a2, a3);
	}

	public int CompareTo(CubiePosition p)
	{
		if (p.a1 != a1)
		{
			return a1 - p.a1;
		}

		if (p.a2 != a2)
		{
			return a2 - p.a2;
		}

		return a3 - p.a3;
	}

	public bool IsCenter()
	{
		return a1 == a2 && a2 == a3;
	}

	public bool IsEdge()
	{
		return (a1 == a2 || a2 == a3)
			&& (!IsCenter());
	}

	public bool IsCorner()
	{
		return a1 < a2 && a2 < a3;
	}

	public void RotateFrontClockwise90Degree()
	{
		a1 = Face.RotateFrontClockwise90Degree(a1);
		a2 = Face.RotateFrontClockwise90Degree(a2);
		a3 = Face.RotateFrontClockwise90Degree(a3);
		AdjustOrder();
	}

	public void MapFrontFrom(sbyte side)
	{
		a1 = FaceLayout3D.MapFrontFrom(side, a1);
		a2 = FaceLayout3D.MapFrontFrom(side, a2);
		a3 = FaceLayout3D.MapFrontFrom(side, a3);
		AdjustOrder();
	}

	public void MapFrontTo(sbyte side)
	{
		a1 = FaceLayout3D.MapFrontTo(side, a1);
		a2 = FaceLayout3D.MapFrontTo(side, a2);
		a3 = FaceLayout3D.MapFrontTo(side, a3);
		AdjustOrder();
	}
}

class CubieColor
{
	/*
	 * For position:
	 *  1. when a1=a2=a3, it is a center cubie
	 *  2. when a1==a2 < a3, it is a edge cubie
	 *  3. when a1<a3<a3, it is is a corner cubie
	 *  */
	public sbyte color, neighborColor1, neighborColor2;

	public CubieColor(sbyte b1, sbyte b2, sbyte b3)
	{
		color = b1;
		neighborColor1 = Math.Min(b2, b3);
		neighborColor2 = Math.Max(b2, b3);
		if (IsEdge())
		{
			//if this is edge facelet, keep neighborColor1 smaller of the two colors
			//this hels compare two CubieColor
			if (neighborColor2 == color)
			{
				Debug.Assert(neighborColor2 != neighborColor1 && neighborColor1 != color);
				neighborColor2 = neighborColor1;
			}

			neighborColor1 = Math.Min(color, neighborColor2);
		}
	}

	public override string ToString()
	{
		//return String.Format("{0}", color);
		if (IsCenter())
		{
			return String.Format(" {0} :", color);
		}
		if (IsEdge())
		{
			return String.Format("{0}{1} :", color, color == neighborColor1 ? neighborColor2 : neighborColor1);
		}
		Debug.Assert(IsCorner());
		return String.Format("{0}{1}{2}:", color, neighborColor1, neighborColor2);
	}

	public CubieColor Clone()
	{
		return new CubieColor(color, neighborColor1, neighborColor2);
	}

	public int CompareTo(CubieColor p)
	{
		if (p.color != color)
		{
			return color - p.color;
		}

		if (p.neighborColor1 != neighborColor1)
		{
			return neighborColor1 - p.neighborColor1;
		}

		return neighborColor2 - p.neighborColor2;
	}

	public bool IsCenter()
	{
		return color == neighborColor1 && neighborColor1 == neighborColor2;
	}

	public bool IsCorner()
	{
		return color != neighborColor1 &&
			color != neighborColor2 &&
			neighborColor1 < neighborColor2;
	}

	public bool IsEdge()
	{
		return !IsCorner() && !IsCenter();
	}
}

//Read the Cube state from Console
class CubeReader
{
	sbyte[,,] m_byte = new sbyte[6, 3, 3];

	public sbyte this[int i, int j, int k]
	{
		get
		{
			return m_byte[i, j, k];
		}
	}

	public Cube ReadCube()
	{
		string example =
			"g g w;r r r;b b r\n" + //0
			"r g g;w g g;y g g\n" + //1
			"b b b;b b b;r r y\n" + //2
			"y y w;y y y;y y r\n" + //3
			"g r b;w w w;w w w\n" + //4
			"p p p;p p p;p p p\n"; //5
								   //solution:step = 62, 
								   //path=XFRUR'U'F'YYLU'R'UL'U'RU2YYLU'R'UL'U'RU2R'U'RU'R'U2RU2RUR'URU2R'U2RUR'URU2R'U2YYR2UFB'R2F'BUR2
		Console.WriteLine("Please input the colors of the cube, for example:\n" +
				example);
		for (int i = 0; i < 6; i++)
		{
			bool ok = false;
			while (!ok)
			{
				ok = true;
				Console.Write("Face {0}:", Face.FaceNames[i]);
				string[] rows = Console.ReadLine().ToLower().Split(';');
				if (rows.Length != 3)
				{
					if (rows[0] == "exit" || rows[0] == "quit")
					{
						return null;
					}
					Console.WriteLine("Expect 3 rows in {0}, actual rows={1}", Face.FaceNames[i], rows.Length);
					ok = false;
					continue;
				}

				for (int j = 0; j < 3; j++)
				{
					//rows[j];
					string[] splitStr = rows[j].Trim().ToUpper().Split(' ');
					if (splitStr.Length != 3)
					{
						Console.WriteLine("Expect 3 cubies in row {0}, actual cubie#={1}, cubies={2}", j, splitStr.Length, rows[j]);
						ok = false;
						continue;
					}
					else
					{
						Console.WriteLine("{0},{1},{2}", splitStr[0], splitStr[1], splitStr[2]);
						for (int k = 0; k < 3; k++)
						{
							string s = splitStr[k].Trim().ToLower();
							if (!Face.ColorToFace.ContainsKey(s))
							{
								string[] validColors = new string[Face.ColorToFace.Keys.Count];
								Face.ColorToFace.Keys.CopyTo(validColors, 0);
								Console.WriteLine("Column {0} [{1}] is not valid color. Valid colors are:{2}",
									k, s, String.Join(",", validColors));
								ok = false;
							}
							else
							{
								m_byte[i, j, k] = Face.ColorToFace[s];
							}
						}
					}
				}
			}
		}

		return Cube.GetCubeFromReader(this);
	}
}

/*
 * 
 * When a Cubie is used as key, it is the position.
 * When it is used as value, it is the facelet(or colors).
 * 
 * */
class Cubie
{
	public CubiePosition position;
	public CubieColor color;
	public Cubie(CubiePosition a, CubieColor b)
	{
		position = a;
		color = b;
	}
}
class Face
{
	/*
	 * A cube has six faces, lets name them 0-5
	 *   0 F = front face
	 *   1 R = right face
	 *   2 U = up face
	 *   3 L = left face
	 *   4 D = down face
	 *   5 B = back face
	 * */
	public const sbyte Front = 0;
	public const sbyte Right = 1;
	public const sbyte Up = 2;
	public const sbyte Left = 3;
	public const sbyte Down = 4;
	public const sbyte Back = 5;

	public static readonly Dictionary<int, string> FaceNames = new Dictionary<int, string>();//{ "Front", "Right", "Up", "Left", "Down", "Back" };
	public static readonly Dictionary<sbyte, ConsoleColor.BackGroundColor> FaceColors = new Dictionary<sbyte, ConsoleColor.BackGroundColor>();
	public static readonly Dictionary<string, sbyte> ColorToFace = new Dictionary<string, sbyte>();
	//Colors on my cube
	static Face()
	{
		AddFaceColorMap(Front, "Front", ConsoleColor.BackGroundColor.Green, "g");
		AddFaceColorMap(Right, "Right", ConsoleColor.BackGroundColor.Red, "r");
		AddFaceColorMap(Up, "Up", ConsoleColor.BackGroundColor.Grey, "w");
		AddFaceColorMap(Left, "Left", ConsoleColor.BackGroundColor.Magenta, "p");
		AddFaceColorMap(Down, "Down", ConsoleColor.BackGroundColor.Yellow, "y");
		AddFaceColorMap(Back, "Back", ConsoleColor.BackGroundColor.Blue, "b");
	}
	private static void AddFaceColorMap(sbyte face, string faceName, ConsoleColor.BackGroundColor color, string colorName)
	{
		FaceNames.Add((int)face, faceName);
		FaceColors.Add(face, color);
		ColorToFace.Add(colorName, face);
	}

	static public ConsoleColor.BackGroundColor GetColorForFace(sbyte face)
	{
		return FaceColors[face];
	}

	/*
	 * If we rotate the front face clockwise, we'll change
	 *          ___
	 *         |   |
	 *         | 2 |
	 *      ___|___|___
	 *     |   |   |   |
	 *     | 3 | 0 | 1 |
	 *     |___|___|___|
	 *         |   |
	 *         | 4 |
	 *         |___|
	 * into         
	 *          ___
	 *         |   |
	 *         | 3 |
	 *      ___|___|___
	 *     |   |   |   |
	 *     | 4 | 0 | 2 |
	 *     |___|___|___|
	 *         |   |
	 *         | 1 |
	 *         |___|
	 * */
	public static sbyte RotateFrontClockwise90Degree(sbyte v)
	{
		switch (v)
		{
			case Face.Front:
				return (sbyte)Face.Front;
			case Face.Right:
				return (sbyte)Face.Down;
			case Face.Down:
				return (sbyte)Face.Left;
			case Face.Left:
				return (sbyte)Face.Up;
			case Face.Up:
				return (sbyte)Face.Right;
			case Face.Back:
				return Face.Back;
			default:
				throw new ApplicationException("invalid input");
		}
	}

	//rotat 270degree clockwise is same as counter clockwise 90 degree
	public static sbyte RotateFrontCounterClockwise90Degree(sbyte v)
	{
		v = RotateFrontClockwise90Degree(v);
		v = RotateFrontClockwise90Degree(v);
		v = RotateFrontClockwise90Degree(v);
		return v;
	}

	private CubieColor FakeColor(sbyte b1)
	{
		return new CubieColor(b1, b1, b1);
	}

	public void ResetFromCubeReader(CubeReader r, FaceLayout2D p, int i)
	{
		this[p.self, p.left, p.up] = FakeColor(r[i, 0, 0]);
		this[p.self, p.self, p.up] = FakeColor(r[i, 0, 1]);
		this[p.self, p.right, p.up] = FakeColor(r[i, 0, 2]);
		this[p.self, p.left, p.self] = FakeColor(r[i, 1, 0]);
		this[p.self, p.self, p.self] = FakeColor(r[i, 1, 1]);
		this[p.self, p.right, p.self] = FakeColor(r[i, 1, 2]);
		this[p.self, p.left, p.down] = FakeColor(r[i, 2, 0]);
		this[p.self, p.self, p.down] = FakeColor(r[i, 2, 1]);
		this[p.self, p.right, p.down] = FakeColor(r[i, 2, 2]);
	}
	public string[] GetFaceString(FaceLayout2D p)
	{
		string[] ret = new string[3];
		ret[0] = String.Format("{0}{1}{2}",
			this[p.self, p.left, p.up],
			this[p.self, p.self, p.up],
			this[p.self, p.right, p.up]
			);
		ret[1] = String.Format("{0}{1}{2}",
			this[p.self, p.left, p.self],
			this[p.self, p.self, p.self],
			this[p.self, p.right, p.self]
			);
		ret[2] = String.Format("{0}{1}{2}",
			this[p.self, p.left, p.down],
			this[p.self, p.self, p.down],
			this[p.self, p.right, p.down]
			);
		return ret;
	}

	public CubieColor this[sbyte a1, sbyte a2, sbyte a3]
	{
		get
		{
			CubiePosition position = new CubiePosition(a1, a2, a3);
			foreach (Cubie cubie in cubies)
			{
				if (cubie.position.CompareTo(position) == 0)
				{
					return cubie.color;
				}
			}
			throw new ApplicationException(String.Format("position {0} not found", position));
		}

		set
		{
			CubiePosition position = new CubiePosition(a1, a2, a3);
			for (int i = 0; i < cubies.Count; i++)
			{
				Cubie cubie = cubies[i];
				if (cubie.position.CompareTo(position) == 0)
				{
					cubie.color = value;
					return;
				}
			}
			throw new ApplicationException(String.Format("position {0} not found in set", position));
		}
	}
	internal List<Cubie> cubies = new List<Cubie>();
	private Face()
	{
	}
	public Face(sbyte front, sbyte right, sbyte up, sbyte left, sbyte down)
	{
		//Each face has 3*3=9 cubies
		AddCubie(front, left, up); //left up corner cubie
		AddCubie(front, left, front); //up edge cubie
		AddCubie(front, left, down);
		AddCubie(front, front, up);
		AddCubie(front, front, front); //center cubie
		AddCubie(front, front, down);
		AddCubie(front, right, up);
		AddCubie(front, right, front);
		AddCubie(front, right, down);
	}

	public void Normalize()
	{
		cubies.Sort((x, y) => x.position.CompareTo(y.position));
	}

	public sbyte Center(bool needPositon)
	{
		foreach (Cubie cubie in cubies)
		{
			CubiePosition c = cubie.position;
			if (c.IsCenter())
			{
				Debug.Assert(cubie.color.IsCenter());
				if (needPositon)
					return cubie.position.a1;
				else
					return cubie.color.color;
			}
		}

		throw new ApplicationException("no center found");
	}

	public int CompareTo(Face other)
	{
		for (int i = 0; i < cubies.Count; i++)
		{
			CubiePosition pos = cubies[i].position;
			int ret = cubies[i].color.CompareTo(other[pos.a1, pos.a2, pos.a3]);
			if (ret != 0)
			{
				return ret;
			}
		}

		return 0;
	}

	private void AddCubie(sbyte a1, sbyte a2, sbyte a3)
	{
		cubies.Add(new Cubie(new CubiePosition(a1, a2, a3), new CubieColor(a1, a2, a3)));
	}
	public Face Clone()
	{
		Face ret = new Face();
		foreach (Cubie cubie in cubies)
		{
			ret.cubies.Add(new Cubie(cubie.position.Clone(), cubie.color.Clone()));
		}

		return ret;
	}

	public void RotateFrontClockwise90Degree()
	{
		foreach (Cubie cubie in cubies)
		{
			cubie.position.RotateFrontClockwise90Degree();
		}
	}

	public void MapFrontFrom(sbyte side)
	{
		foreach (Cubie cubie in cubies)
		{
			cubie.position.MapFrontFrom(side);
		}
	}

	public void MapFrontTo(sbyte side)
	{
		foreach (Cubie cubie in cubies)
		{
			cubie.position.MapFrontTo(side);
		}
	}
}

class RubikPattern
{
	public string description;
	string[] steps;
	public RubikPattern(string d, string[] s)
	{
		description = d;
		steps = s;
	}

	public MoveHistory GetPattern()
	{
		MoveHistory ret = new MoveHistory(Move.ApplyActions(Cube.GetInitState(), steps));
		Console.WriteLine("Set cube to pattern: " + description);
		return ret;
	}

	public static List<RubikPattern> patterns = new List<RubikPattern>();
	static void AddPattern(string description, string moves)
	{
		patterns.Add(new RubikPattern(description, moves.Split(' ')));
	}
	public static MoveHistory GetPattern(int index)
	{
		if (index >= patterns.Count)
		{
			index = 0;
		}
		return patterns[index].GetPattern();
	}
	static RubikPattern()
	{
		AddPattern("initial state", "");
		//patterns listed in http://kociemba.org/cube.htm
		AddPattern("Pretty Pattern:Superflip", "R2 F B R B2 R U2 L B2 R U' D' R2 F D2 B2 U2 R' L U");
		AddPattern("Pretty Pattern:Colored Anaconda 1", "F' U' L' B' F' R' U' D B R2 U B R");
		AddPattern("Pretty Pattern:Colored Anaconda 2", "F R D F2 R' U' D B L' R' F' D' R'");
		AddPattern("Pretty Pattern:Colored Phyton 1", "D' L' R' B F D' U' L' R' B' F' U'");
		AddPattern("Pretty Pattern:Colored Phyton 2", "D' L' R' B' F' D U L R B' F' U'");
		AddPattern("Pretty Pattern:Two Colored Rings", "L U' B F' L D' U' R B F' U' R");
		AddPattern("Pretty Pattern:Six Square Blocks", "F2 D F2 D2 L2 U L2 U' L2 B D2 R2");
		AddPattern("Pretty Pattern:Pons Asinorum", "U2 D2 F2 B2 R2 L2");
		AddPattern("Pretty Pattern:Pons Asinorum composed with Superflip", "B' D' L' F' D' F' B U F' B R2 L U D' F L U R D");
		AddPattern("Schoenflies-Symbol Th", "U2 L2 F2 D2 U2 F2 R2 U2");
		AddPattern("Schoenflies-Symbol T", "B F L R B' F' D' U' L R D U");
		AddPattern("Schoenflies-Symbol D3d", "U L D U L' D' U' R B2 U2 B2 L' R' U'");
		AddPattern("Schoenflies-Symbol C3v", "U L' R' B2 U' R2 B L2 D' F2 L' R' U'");
		AddPattern("Schoenflies-Symbol D3", "D B D U2 B2 F2 L2 R2 U' F U");
		AddPattern("Schoenflies-Symbol S6", "B' D' U L' R B' F U");
		AddPattern("Schoenflies-Symbol C3", "L' R U2 R2 D2 F2 L R D2");
		AddPattern("Schoenflies-Symbol D4h", "U2 D2");
		AddPattern("Schoenflies-Symbol D4h", "U D");
		AddPattern("Schoenflies-Symbol S4", "U R2 L2 U2 R2 L2 D");
		AddPattern("Schoenflies-Symbol D2d (edge)", "U F2 B2 D2 F2 B2 U");
		AddPattern("Schoenflies-Symbol D2d (face)", "U R L F2 B2 R' L' U");
		AddPattern("Schoenflies-Symbol D2h (edge)", "U R2 L2 D2 F2 B2 U");
		AddPattern("Schoenflies-Symbol D2h(face)", "B2 D2 U2 F2");
		AddPattern("Schoenflies-Symbol D2 (edge)", "U F2 U2 D2 F2 D");
		AddPattern("Schoenflies-Symbol D2 (face)", "R2 L2 F B");
		AddPattern("Schoenflies-Symbol C2v (a1)", "U R2 L2 U2 F2 B2 U'");
		AddPattern("Schoenflies-Symbol C2v (a2)", "R2 L2 U2");
		AddPattern("Schoenflies-Symbol C2v (b)", "B2 R2 B2 R2 B2 R2");
		AddPattern("Schoenflies-Symbol C2h (a)", "U' D F2 B2");
		AddPattern("Schoenflies-Symbol C2h (b)", "U R2 U D R2 D");
		AddPattern("Schoenflies-Symbol C2 (a)", "L R U2");
		AddPattern("Schoenflies-Symbol C2 (b)", "U R2 D' U' R2 U'");
		AddPattern("Schoenflies-Symbol Cs (b)", "U B2 U D B2 D'");
		AddPattern("Schoenflies-Symbol Ci", "U D' R L'");
		//moves listed in http://peter.stillhq.com/jasmine/rubikscubesolution.html
		AddPattern("Beginner Practice:Middle layer1:Move (4,0,0) to (3,0,0), and keep (0,0,4) in face 0", "D L D' L' D' F' D F");
		AddPattern("Beginner Practice:Middle layer2:Move (4,3,3) to (0,0,3), and keep (3,4) in face 3", "D' F' D F D L D' L'");
		AddPattern("Beginner Practice:Last layer:Orienting LL Edges: state2(L)", "F U R U' R' F'");
		AddPattern("Beginner Practice:Last layer:Orienting LL Edges: state3(-)", "F R U R' U' F'");
		AddPattern("Beginner Practice:Last layer:Swapping adjacent corners", "L U' R' U L' U' R U2");
		AddPattern("Beginner Practice:State 1. Twisting three corners anti-clockwise", "R' U' R U' R' U2 R U2");
		AddPattern("Beginner Practice:State 2. Twisting three corners clockwise", "R U R' U R U2 R' U2");
		AddPattern("Beginner Practice:Permuting the LL Edges step1", "R2 U F B' R2 F' B U R2");
		AddPattern("Beginner Practice:Permuting the LL Edges step2", "R2 U' F B' R2 F' B U' R2");
	}
}

class MoveHistory
{
	int step = 0;
	List<Cube> list = new List<Cube>();
	public MoveHistory(Cube initState)
	{
		Add(initState);
	}

	public void Add(Cube state)
	{
		step++;
		list.Add(state);
	}

	public Cube FindExactMatch(Cube state)
	{
		return list.Find((obj) => obj.CompareTo(state) == 0);
	}

	//If we rotate the whole cube to another cube, then they are similar
	public Cube FindSimilarMatch(Cube state)
	{
		return list.Find((obj) => obj.Similar(state));
	}

	public void AddToHistoryIfNotDuplicate(Cube state)
	{
		Cube oldOne = FindExactMatch(state);
		if (oldOne != null)
		{
			state.Print();
			using (new ConsoleColor((int)ConsoleColor.ForeGroundColor.Yellow | 0x8))
			{
				Console.WriteLine("the above is duplicate state to step={0}, path={1}, ignored",
					oldOne.step, oldOne.path);
			}
		}
		else
		{
			oldOne = FindSimilarMatch(state);
			if (oldOne != null)
			{
				using (new ConsoleColor((int)ConsoleColor.ForeGroundColor.Yellow))
				{
					Console.WriteLine("Warning: the above is simliar state to step={0}, path={1}",
						oldOne.step, oldOne.path);
				}
			}
			Add(state);
			PrintLastState();
		}
	}

	public void Undo()
	{
		if (step > 1)
		{
			Cube s = list[step - 1];
			list.RemoveAt(step - 1);
			step--;
		}
		else
		{
			Console.WriteLine("Cannot undo the initial state");
		}
	}

	public Cube GetStartState()
	{
		return list[0];
	}

	public Cube GetCurrent()
	{
		if (step > 0)
		{
			return list[step - 1];
		}
		else
		{
			throw new ApplicationException("no current value");
		}
	}

	public void PrintHistory()
	{
		foreach (Cube s in list)
		{
			s.Print();
		}
	}
	public void PrintLastState()
	{
		GetCurrent().Print();
	}
}
