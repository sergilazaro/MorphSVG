using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Xml;
using System.Threading;
using System.Diagnostics;

namespace MorphSVG
{
	class Path
	{
		static float Lerp(float f1, float f2, float t)
		{
			return f1 + (f2 - f1) * t;
		}

		public class Movement
		{
			public float x, y;

			public static Movement FromRelative(Movement prev, float x, float y)
			{
				return new Movement { x = prev.x + x, y = prev.y + y };
			}

			public static Movement Lerp(Movement m1, Movement m2, float t)
			{
				return new Movement
				{
					x = Path.Lerp(m1.x, m2.x, t),
					y = Path.Lerp(m1.y, m2.y, t)
				};
			}

			public override string ToString()
			{
				return "M " + x + " " + y;
			}
		}

		public class Line : Movement
		{
			public static new Cubic FromRelative(Movement prev, float x, float y)
			{
				float mid_x = prev.x + x / 2;
				float mid_y = prev.y + y / 2;

				return new Cubic {
					x = prev.x + x,
					y = prev.y + y,
					c1x = mid_x,
					c1y = mid_y,
					c2x = mid_x,
					c2y = mid_y
				};
			}

			public static Line Lerp(Line m1, Line m2, float t)
			{
				return new Line
				{
					x = Path.Lerp(m1.x, m2.x, t),
					y = Path.Lerp(m1.y, m2.y, t)
				};
			}

			public override string ToString()
			{
				return "L " + x + " " + y;
			}
		}

		public class Quadratic : Movement
		{
			public float cx, cy;

			public static Quadratic FromRelative(Movement prev, float cx, float cy, float x, float y)
			{
				return new Quadratic { cx = prev.x + cx, cy = prev.y + cy, x = prev.x + x, y = prev.y + y };
			}

			public static Quadratic Lerp(Quadratic m1, Quadratic m2, float t)
			{
				return new Quadratic
				{
					x = Path.Lerp(m1.x, m2.x, t),
					y = Path.Lerp(m1.y, m2.y, t),
					cx = Path.Lerp(m1.cx, m2.cx, t),
					cy = Path.Lerp(m1.cy, m2.cy, t)
				};
			}

			public override string ToString()
			{
				return "Q " + cx + " " + cy + " " + x + " " + y;
			}
		}

		public class Cubic : Movement
		{
			public float c1x, c1y;
			public float c2x, c2y;

			public static Cubic FromRelative(Movement prev, float c1x, float c1y, float c2x, float c2y, float x, float y)
			{
				return new Cubic { c1x = prev.x + c1x, c1y = prev.y + c1y, c2x = prev.x + c2x, c2y = prev.y + c2y, x = prev.x + x, y = prev.y + y };
			}

			public static Cubic Lerp(Cubic m1, Cubic m2, float t)
			{
				return new Cubic
				{
					x = Path.Lerp(m1.x, m2.x, t),
					y = Path.Lerp(m1.y, m2.y, t),
					c1x = Path.Lerp(m1.c1x, m2.c1x, t),
					c1y = Path.Lerp(m1.c1y, m2.c1y, t),
					c2x = Path.Lerp(m1.c2x, m2.c2x, t),
					c2y = Path.Lerp(m1.c2y, m2.c2y, t)
				};
			}

			public override string ToString()
			{
				return "C " + c1x + " " + c1y + " " + c2x + " " + c2y + " " + x + " " + y;
			}
		}

		public class Arc : Movement
		{
			public float rx, ry;
			public string xAxisRotate;
			public bool LargeArcFlag, SweepFlag;

			public static Arc FromRelative(Movement prev, float rx, float ry, string xAxisRotate, bool LargeArcFlag, bool SweepFlag, float x, float y)
			{
				return new Arc { x = prev.x + x, y = prev.y + y, rx = rx, ry = ry, xAxisRotate = xAxisRotate, LargeArcFlag = LargeArcFlag, SweepFlag = SweepFlag };
			}

			public static Arc Lerp(Arc m1, Arc m2, float t)
			{
				return new Arc
				{
					x = Path.Lerp(m1.x, m2.x, t),
					y = Path.Lerp(m1.y, m2.y, t),
					rx = Path.Lerp(m1.rx, m2.rx, t),
					ry = Path.Lerp(m1.ry, m2.ry, t),
					// ignore this unused value
					xAxisRotate = m1.xAxisRotate,
					// keep the rest
					LargeArcFlag = m1.LargeArcFlag,
					SweepFlag = m1.SweepFlag
				};
			}

			public override string ToString()
			{
				return "A " + rx + " " + ry + " " + xAxisRotate + " " + (LargeArcFlag?1:0) + " " + (SweepFlag?1:0) + " " + x + " " + y;
			}
		}

		public List<Movement> movements = new List<Movement>();

		public void AddMovement(float x, float y, bool relative)
		{
			if (relative && movements.Count == 0)
				relative = false;

			if (relative)
				movements.Add(Movement.FromRelative(movements.Last(), x, y));
			else
				movements.Add(new Movement { x = x, y = y });
		}

		public void AddLine(float x, float y, bool relative)
		{
			if (relative && movements.Count == 0)
				throw new InvalidDataException();

			if (relative)
				movements.Add(Line.FromRelative(movements.Last(), x, y));
			else
			{
				float mid_x = (movements.Last().x + x) / 2;
				float mid_y = (movements.Last().y + y) / 2;
				movements.Add(new Cubic
				{
					x = x,
					y = y,
					c1x = mid_x,
					c1y = mid_y,
					c2x = mid_x,
					c2y = mid_y
				});
			}
		}

		public void AddQuadratic(float cx, float cy, float x, float y, bool relative)
		{
			if (relative && movements.Count == 0)
				throw new InvalidDataException();

			if (relative)
				movements.Add(Quadratic.FromRelative(movements.Last(), cx, cy, x, y));
			else
				movements.Add(new Quadratic { x = x, y = y, cx = cx, cy = cy });
		}

		public void AddCubic(float c1x, float c1y, float c2x, float c2y, float x, float y, bool relative)
		{
			if (relative && movements.Count == 0)
				throw new InvalidDataException();

			if (relative)
				movements.Add(Cubic.FromRelative(movements.Last(), c1x, c1y, c2x, c2y, x, y));
			else
				movements.Add(new Cubic { x = x, y = y, c1x = c1x, c1y = c1y, c2x = c2x, c2y = c2y });
		}

		public void AddArc(float rx, float ry, string xAxisRotate, bool LargeArcFlag, bool SweepFlag, float x, float y, bool relative)
		{
			if (relative && movements.Count == 0)
				throw new InvalidDataException();

			if (relative)
				movements.Add(Arc.FromRelative(movements.Last(), rx, ry, xAxisRotate, LargeArcFlag, SweepFlag, x, y));
			else
				movements.Add(new Arc { x = x, y = y, rx = rx, ry = ry, xAxisRotate = xAxisRotate, LargeArcFlag = LargeArcFlag, SweepFlag = SweepFlag });
		}

		public static Path Lerp(Path p1, Path p2, float t)
		{
			Path path = new Path();

			if (p1.movements.Count != p2.movements.Count)
				throw new Exception("Paths with different number of movements");

			for (int i = 0; i < p1.movements.Count; i++)
			{
				Movement m1 = p1.movements[i];
				Movement m2 = p2.movements[i];

				if (m1.GetType() != m2.GetType())
					throw new Exception("Paths with different type of movements");

				string type = m1.GetType().ToString().Split('+')[1].Trim();
				switch (type)
				{
					case "Movement":
						path.movements.Add(Movement.Lerp(m1 as Movement, m2 as Movement, t));
						break;

					case "Line":
						path.movements.Add(Line.Lerp(m1 as Line, m2 as Line, t));
						break;

					case "Quadratic":
						path.movements.Add(Quadratic.Lerp(m1 as Quadratic, m2 as Quadratic, t));
						break;

					case "Cubic":
						path.movements.Add(Cubic.Lerp(m1 as Cubic, m2 as Cubic, t));
						break;

					case "Arc":
						path.movements.Add(Arc.Lerp(m1 as Arc, m2 as Arc, t));
						break;

					default:
						throw new Exception("Unknown movement type");

				}
			}

			return path;
		}

		public static Path Parse(string d)
		{
			var tokens = d.Split(new char[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries);
			Path path = new Path();

			string lastType = "";
			int current = 0;
			while (current < tokens.Count())
			{
				string token = tokens[current].Trim();

				string type;
				float val;
				if (float.TryParse(token, out val))
				{
					switch (lastType)
					{
						case "q":
						case "Q":
						case "c":
						case "C":
						case "L":
						case "l":
							// continue 
							type = lastType;
							current--;
							break;

						case "M":
							type = "L";
							current--;
							break;
						case "m":
							type = "l";
							current--;
							break;

						default:
							throw new Exception("Unknown syntax");

					}
				}
				else
				{
					type = token;
				}
				
				switch (type)
				{
					case "M":
						path.AddMovement(
							float.Parse(tokens[current + 1]),
							float.Parse(tokens[current + 2]),
							false);
						current += 3;
						break;
					case "m":
						path.AddMovement(
							float.Parse(tokens[current + 1]),
							float.Parse(tokens[current + 2]),
							true);
						current += 3;
						break;

					case "L":
						path.AddLine(
							float.Parse(tokens[current + 1]),
							float.Parse(tokens[current + 2]),
							false);
						current += 3;
						break;
					case "l":
						path.AddLine(
							float.Parse(tokens[current + 1]),
							float.Parse(tokens[current + 2]),
							true);
						current += 3;
						break;


					case "Q":
						path.AddQuadratic(
							float.Parse(tokens[current + 1]),
							float.Parse(tokens[current + 2]),
							float.Parse(tokens[current + 3]),
							float.Parse(tokens[current + 4]),
							false);
						current += 5;
						break;
					case "q":
						path.AddQuadratic(
							float.Parse(tokens[current + 1]),
							float.Parse(tokens[current + 2]),
							float.Parse(tokens[current + 3]),
							float.Parse(tokens[current + 4]),
							true);
						current += 5;
						break;

					case "C":
						path.AddCubic(
							float.Parse(tokens[current + 1]),
							float.Parse(tokens[current + 2]),
							float.Parse(tokens[current + 3]),
							float.Parse(tokens[current + 4]),
							float.Parse(tokens[current + 5]),
							float.Parse(tokens[current + 6]),
							false);
						current += 7;
						break;
					case "c":
						path.AddCubic(
							float.Parse(tokens[current + 1]),
							float.Parse(tokens[current + 2]),
							float.Parse(tokens[current + 3]),
							float.Parse(tokens[current + 4]),
							float.Parse(tokens[current + 5]),
							float.Parse(tokens[current + 6]),
							true);
						current += 7;
						break;

					case "A":
						path.AddArc(
							float.Parse(tokens[current + 1]),
							float.Parse(tokens[current + 2]),
							tokens[current + 3],
							int.Parse(tokens[current + 4]) == 1,
							int.Parse(tokens[current + 5]) == 1,
							float.Parse(tokens[current + 6]),
							float.Parse(tokens[current + 7]),
							false);
						current += 8;
						break;
					case "a":
						path.AddArc(
							float.Parse(tokens[current + 1]),
							float.Parse(tokens[current + 2]),
							tokens[current + 3],
							int.Parse(tokens[current + 4]) == 1,
							int.Parse(tokens[current + 5]) == 1,
							float.Parse(tokens[current + 6]),
							float.Parse(tokens[current + 7]),
							true);
						current += 8;
						break;

					case "Z":
					case "z":
						path.AddLine(
							path.movements[0].x,
							path.movements[0].y,
							false);
						current += 1;
						break;

					default:
						throw new Exception("Unknown movement type");
				}

				lastType = type;
			}

			return path;
		}

		public override string ToString()
		{
			string str = "";

			foreach (Movement movement in movements)
			{
				str += movement.ToString() + " ";
			}

			return str.Trim();
		}
	}

	class Program
	{
		public static readonly string INKSCAPE_PATH = @"C:\Program Files\Inkscape\inkscape.exe";
		public static readonly string IMAGEMAGICK_PATH = @"C:\Program Files\ImageMagick-7.0.5-Q16\magick.exe";
		public static readonly string TEMP_FOLDER = System.IO.Path.GetTempPath();
		public static readonly string TEMP_FILENAME = "MorphSVG";
		
		public static float HermiteEase(float normalizedInput)
		{
			float normalizedInput_squared = normalizedInput * normalizedInput;

			return 3 * normalizedInput_squared - 2 * normalizedInput * normalizedInput_squared;
		}

		static void Main(string[] args)
		{
			Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;
			Thread.CurrentThread.CurrentUICulture = System.Globalization.CultureInfo.InvariantCulture;

			if (args.Count() != 1)
			{
				Console.Error.WriteLine("One argument needed: config file");
				Environment.Exit(-1);
				return;
			}

			string svg_filename = "";
			List<Tuple<string, string>> morph_pairs = new List<Tuple<string, string>>();
			string base_filename = "";
			int num_frames = 60;
			int delay_cents = 2;
			string ease = "linear";
			bool loop = true;
			string resize_percent = "20";
			int num_repeat_end_frames = 1;

			foreach (string line in File.ReadLines(args[0]))
			{
				if (line.Trim().Count() == 0)
					continue;

				if (line.Trim()[0] == '#')
					continue;

				var tokens = line.Split(new char[] { ':' }, 2);

				if (tokens.Count() != 2)
					continue;

				string id = tokens[0].Trim().ToLower();
				string value = tokens[1].Trim();

				switch (id)
				{
					case "svg":
						svg_filename = value;
						break;

					case "morph":
						{
							var inner_tokens = value.Split(new char[] { ',' }, 2);
							morph_pairs.Add(new Tuple<string, string>(inner_tokens[0].Trim(), inner_tokens[1].Trim()));
						}
						break;

					case "frames":
						num_frames = int.Parse(value);
						break;

					case "base filename":
						base_filename = value;
						break;

					case "delay cents":
						delay_cents = int.Parse(value);
						break;

					case "ease":
						ease = value;
						break;

					case "loop":
						loop = bool.Parse(value);
						break;

					case "resize percent":
						resize_percent = value;
						break;

					case "repeat end frames":
						num_repeat_end_frames = int.Parse(value);
						break;

					default:
						Console.Error.WriteLine("ERROR: Unidentified key \"" + id + "\"");
						break;
				}
			}

			string folder = System.IO.Path.GetDirectoryName(svg_filename);

			XmlDocument doc = new XmlDocument();
			XmlDocument out_doc = new XmlDocument();
			doc.Load(svg_filename);
			out_doc.Load(svg_filename);

			Dictionary<string, XmlNode> found_nodes = new Dictionary<string, XmlNode>();
			Dictionary<string, XmlNode> out_found_nodes = new Dictionary<string, XmlNode>();

			List<string> AVAILABLE_TYPES = new List<string> { "path", "circle", "ellipse" };

			// in
			foreach (string type in AVAILABLE_TYPES)
				foreach (XmlNode node in doc.GetElementsByTagName(type))
					found_nodes[node.Attributes["id"].Value] = node;

			// out
			foreach (string type in AVAILABLE_TYPES)
				foreach (XmlNode node in out_doc.GetElementsByTagName(type))
					out_found_nodes[node.Attributes["id"].Value] = node;

			// remove out data
			{
				List<string> out_svgs = new List<string>(Directory.GetFiles(TEMP_FOLDER, TEMP_FILENAME + "_*.svg"));
				List<string> out_pngs = new List<string>(Directory.GetFiles(TEMP_FOLDER, TEMP_FILENAME + "_*.png"));
				List<string> delete_files = out_svgs;
				delete_files.AddRange(out_pngs);

				foreach (string file in delete_files)
					File.Delete(file);
			}

			for (int num_frame = 0; num_frame < num_frames; num_frame++)
			{
				float t = num_frame / (float)(num_frames - 1);
				int percent = (int)(100 * (num_frame / (float)num_frames));

				Console.Out.WriteLine("" + percent + "%: Frame " + num_frame + " of " + num_frames);

				switch (ease)
				{
					case "hermite":
						t = HermiteEase(t);
						break;
				}

				foreach (var pair in morph_pairs)
				{
					string id1 = pair.Item1;
					string id2 = pair.Item2;

					if (!found_nodes.ContainsKey(id1))
					{
						Console.Error.WriteLine("ERROR: Didn't find item with ID \"" + id1 + "\"");
						continue;
					}

					if (!found_nodes.ContainsKey(id2))
					{
						Console.Error.WriteLine("ERROR: Didn't find item with ID \"" + id2 + "\"");
						continue;
					}

					XmlNode node1 = found_nodes[id1];
					XmlNode node2 = found_nodes[id2];
					XmlNode out_node1 = out_found_nodes[id1];
					XmlNode out_node2 = out_found_nodes[id2];

					string type1 = node1.Name;
					string type2 = node2.Name;

					if (type1 != type2)
					{
						Console.Error.WriteLine("ERROR: ID \"" + id1 + "\" and ID \"" + id2 + "\" have different types");
						continue;
					}

					switch (type1)
					{
						case "circle":
						case "ellipse":
							{
								float cx1 = float.Parse(node1.Attributes["cx"].InnerText);
								float cy1 = float.Parse(node1.Attributes["cy"].InnerText);
								float cx2 = float.Parse(node2.Attributes["cx"].InnerText);
								float cy2 = float.Parse(node2.Attributes["cy"].InnerText);

								cx2 = cx1 + (cx2 - cx1) * t;
								cy2 = cy1 + (cy2 - cy1) * t;

								out_node2.Attributes["cx"].InnerText = cx2.ToString();
								out_node2.Attributes["cy"].InnerText = cy2.ToString();
							}
							break;

						case "path":
							{
								string d1 = node1.Attributes["d"].InnerText;
								string d2 = node2.Attributes["d"].InnerText;

								Path p1 = Path.Parse(d1);
								Path p2 = Path.Parse(d2);

								Path pInt = Path.Lerp(p1, p2, t);

								out_node2.Attributes["d"].InnerText = pInt.ToString();
							}
							break;

						default:
							Console.WriteLine("WARNING: ID \"" + id1 + "\" with type \"" + type1 + "\" not yet supported");
							break;
					}

					// hide out_node1
					{
						string style = out_node1.Attributes["style"].Value;

						if (!style.Contains("display:none"))
						{
							if (style.Contains("display:inline"))
								style = style.Replace("display:inline", "display:none");
							else
								style = style + ";display:none";
						}

						out_node1.Attributes["style"].Value = style;
					}
				}

				string str_num_frame = num_frame.ToString("D6");
				string out_svg_filename = System.IO.Path.Combine(TEMP_FOLDER, TEMP_FILENAME + "_frame" + str_num_frame + ".svg");
				string out_png_filename = System.IO.Path.Combine(TEMP_FOLDER, TEMP_FILENAME + "_frame" + str_num_frame + ".png");

				out_doc.Save(out_svg_filename);

				// render svg to png
				{
					ProcessStartInfo processInfo;
					processInfo = new ProcessStartInfo();
					processInfo.FileName = INKSCAPE_PATH;
					processInfo.Arguments = "-z \"" + out_svg_filename + "\" -e \"" + out_png_filename + "\"";

					Process.Start(processInfo).WaitForExit();
				}

				File.Delete(out_svg_filename);
			}

			// delete svgs
			{
				List<string> out_svgs = new List<string>(Directory.GetFiles(TEMP_FOLDER, TEMP_FILENAME + "_*.svg"));

				foreach (string file in out_svgs)
					File.Delete(file);
			}

			if (loop)
			{
				foreach (string png in Directory.GetFiles(TEMP_FOLDER, TEMP_FILENAME + "_*.png"))
				{
					var tokens = png.Split('.');
					tokens = tokens[tokens.Length-2].Split('_');
					int value = int.Parse(tokens.Last().Split(new string[] { "frame" }, StringSplitOptions.RemoveEmptyEntries)[0]);
					string str_value = value.ToString("D6");

					if (value == 0)
					{
						for (int i = 0; i < num_repeat_end_frames-1; i++)
						{
							int new_value = 2 * num_frames + num_repeat_end_frames - 1 + i;
							string str_new_value = new_value.ToString("D6");

							File.Copy(
								System.IO.Path.Combine(TEMP_FOLDER, TEMP_FILENAME + "_frame" + str_value + ".png"),
								System.IO.Path.Combine(TEMP_FOLDER, TEMP_FILENAME + "_frame" + str_new_value + ".png")
							);
						}
					}
					else if (value == num_frames - 1)
					{
						for (int i = 0; i < num_repeat_end_frames - 1; i++)
						{
							int new_value = value + i + 1;
							string str_new_value = new_value.ToString("D6");

							File.Copy(
								System.IO.Path.Combine(TEMP_FOLDER, TEMP_FILENAME + "_frame" + str_value + ".png"),
								System.IO.Path.Combine(TEMP_FOLDER, TEMP_FILENAME + "_frame" + str_new_value + ".png")
							);
						}
					}
					else
					{
						int new_value = 2 * num_frames - value + num_repeat_end_frames - 3;
						string str_new_value = new_value.ToString("D6");

						File.Copy(
							System.IO.Path.Combine(TEMP_FOLDER, TEMP_FILENAME + "_frame" + str_value + ".png"),
							System.IO.Path.Combine(TEMP_FOLDER, TEMP_FILENAME + "_frame" + str_new_value + ".png")
						);
					}
				}
			}

			// generate gif
			{
				string out_gif_filename  = System.IO.Path.Combine(folder, base_filename + ".gif");
				string out_pngs_filename = System.IO.Path.Combine(TEMP_FOLDER, TEMP_FILENAME + "_*.png");
				// magick convert -resize 20% -delay 20 -loop 0 *.png test.gif

				ProcessStartInfo processInfo;
				processInfo = new ProcessStartInfo();
				processInfo.FileName = "\"" + IMAGEMAGICK_PATH + "\"";
				processInfo.Arguments = "convert -resize " + resize_percent + "% -delay " + delay_cents + " -loop 0 \"" + out_pngs_filename + "\" \"" + out_gif_filename + "\"";
				processInfo.RedirectStandardOutput = true;
				processInfo.RedirectStandardError = true;
				processInfo.UseShellExecute = false;
				processInfo.CreateNoWindow = true;

				Console.WriteLine(processInfo.FileName + " " + processInfo.Arguments);
				Process process = Process.Start(processInfo);
				process.WaitForExit();
			}

			// delete pngs
			{
				List<string> out_pngs = new List<string>(Directory.GetFiles(TEMP_FOLDER, TEMP_FILENAME + "_*.png"));

				foreach (string file in out_pngs)
					File.Delete(file);
			}
		}
	}
}
