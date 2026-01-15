using NaughtyAttributes;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

[Serializable]
public class Objective
{
	public ShapeKind/*?*/ ShapeKind;
	public int Count;
	public int Target;
	//public NodeColor? ColorObjective;
	//public int? SizeObjective;
}

public class TriangleGrid : MonoBehaviour
{
	public class SimpleShape
	{
		public List<Node> Nodes;

		public SimpleShape(Node[] nodes)
		{
			Nodes = new List<Node>(nodes);
		}

		public bool EqualsShape(SimpleShape otherShape)
		{
			foreach (var node in Nodes)
			{
				if (!otherShape.Nodes.Contains(node))
				{
					return false;
				}
			}

			return true;
		}
	}

	public enum TriangleOrientation
	{
		Vertical,
		Horizontal
	}

	[SerializeField] private GameObject _shapeTemplate = null;
	[SerializeField] private GameObject _nodePrefab = null;
	[SerializeField] private float _nodeDistance = 0f;
	[SerializeField] private string[] _progression = null;
	[SerializeField] private float _computationInterval = 0f;
	[SerializeField] private Player _player = null;

	public float NodeDistance => _nodeDistance;

	public Vector2 Size { get; set; }
	public List<Node> Nodes { get; set; }
	public List<Shape> Shapes { get; set; }
	public NodeColor[] ColorOptions { get; set; }
	public Dictionary<NodeColor, Color> NodeColorMap { get; set; }
	public Dictionary<ShapeKind, Objective> Objectives { get; set; }
	public bool CustomGrid { get; set; }

	private ShapeGenerator _shapeGenerator;
	private ShapeKind[] _shapeKinds;
	private NodeColor[] _nodeColors;
	private int? _level;
	private int _width;
	private int _height;
	private int _colors;
	private float _objectiveRatio;

	private TriangleOrientation _triangleOrientation;
	private Color[] _colorPalette;
	private bool _completed;
	private List<SimpleShape> _validShapes;
	private CancellationTokenSource _computeShapeCancellationTokenSource;
	private string _configurationString;
	private string _progressString;

#if UNITY_WEBGL
	private int _computationCount;
	private int _cachedComputationCount;
	private float _computationTimer;
#endif
	
	//private int _triangles;
	//private int _diamonds;
	//private int _hexagons;
	//private bool _doneComputingShapes;

	private void Awake()
	{
		_shapeGenerator = GetComponent<ShapeGenerator>();
		_shapeKinds = Enum.GetValues(typeof(ShapeKind)) as ShapeKind[];
		_nodeColors = Enum.GetValues(typeof(NodeColor)) as NodeColor[];
	}

#if UNITY_WEBGL
	private void Update()
	{
		if (_computationTimer >= 0f)
		{
			_computationTimer -= Time.deltaTime;
		}
	}
#endif
	
	//private IEnumerator MassiveInitialize()
	//{
	//	var resultString = "";

	//	for (var x = 10; x <= 11; x++)
	//	{
	//		for (var y = 3; y <= 7; y++)
	//		{
	//			for (var z = 2; z <= 4; z++)
	//			{
	//				var grids = 200;

	//				_triangles = 0;
	//				_diamonds = 0;
	//				_hexagons = 0;

	//				for (var n = 0; n < grids; n++)
	//				{
	//					CleanUp();

	//					_doneComputingShapes = false;

	//					Initialize(x, y, z, null);

	//					yield return new WaitUntil(() => _doneComputingShapes);

	//					_triangles += _validShapes.Where(shape => shape.Nodes.Count == 3).Count();
	//					_diamonds += _validShapes.Where(shape => shape.Nodes.Count == 4).Count();
	//					_hexagons += _validShapes.Where(shape => shape.Nodes.Count == 6).Count();

	//					yield return null;
	//				}

	//				var triangleAverage = (float)_triangles / (float)grids;
	//				var diamondAverage = (float)_diamonds / (float)grids;
	//				var hexagonAverage = (float)_hexagons / (float)grids;

	//				resultString += $"{triangleAverage};{diamondAverage};{hexagonAverage}\n";

	//				/*Debug.Log($"({x},{y},{z}) (n = {grids})\n" +
	//					$"Triangles : {triangleAverage}\n" +
	//					$"Diamonds : {diamondAverage}\n" +
	//					$"Hexagons : {hexagonAverage}\n" +
	//					$"Total : {triangleAverage + diamondAverage + hexagonAverage}");*/

	//				yield return null;
	//			}
	//		}
	//	}

	//	Debug.Log(resultString);
	//}

	public void Initialize(int progressionIndex)
	{
		var progressionValues = (progressionIndex < _progression.Length
			? _progression[progressionIndex]
			: _progression[_progression.Length - 1]
		).Split(' ');

		if (progressionValues.Length < 2)
		{
			Initialize(progressionValues[0], false);
		}
		else
		{
			_level = progressionIndex + 1;

			var width = int.Parse(progressionValues[0], CultureInfo.InvariantCulture);
			var height = int.Parse(progressionValues[1], CultureInfo.InvariantCulture);
			var colors = int.Parse(progressionValues[2], CultureInfo.InvariantCulture);
			var objectiveRatio = float.Parse(progressionValues[3], CultureInfo.InvariantCulture);

			Initialize(width, height, colors, objectiveRatio, false);
		}
	}

	public void Initialize(int width, int height, int colors, float objectiveRatio, bool custom)
	{
		_width = width;
		_height = height;
		_colors = colors;
		_objectiveRatio = objectiveRatio;

		CustomGrid = custom;

		try
		{
#if UNITY_WEBGL
			StartCoroutine(Initialize());
#else
			_ = Initialize();
#endif
		}
		catch (Exception e)
		{
			Debug.LogWarning(e);

			throw;
		}
	}

#if UNITY_WEBGL
	public IEnumerator Initialize()
#else
	public async Task Initialize()
#endif
	{
		_completed = false;

		_triangleOrientation = _height % 2 == 0 && _width % 2 != 0
			? TriangleOrientation.Horizontal
			: _width % 2 == 0 && _height % 2 != 0
				? TriangleOrientation.Vertical
				: UnityEngine.Random.Range(0f, 1f) > 0.5f
					? TriangleOrientation.Horizontal
					: TriangleOrientation.Vertical;

		_colorPalette = SettingsPanel.Instance.SelectedColorPalette;

		NodeColorMap = new Dictionary<NodeColor, Color>();

		for (var i = 0; i < _nodeColors.Length; i++)
		{
			NodeColorMap.Add(_nodeColors[i], _colorPalette[i]);
		}

		var colorOptions = NodeColorMap.Keys.ToList();

		ColorOptions = new NodeColor[_colors];

		for (var i = 0; i < _colors; i++)
		{
			var colorOption = colorOptions[UnityEngine.Random.Range(0, colorOptions.Count)];
			colorOptions.Remove(colorOption);

			ColorOptions[i] = colorOption;
		}

		var minX = float.MaxValue;
		var maxX = float.MinValue;
		var minY = float.MaxValue;
		var maxY = float.MinValue;

		Nodes = new List<Node>();
		Shapes = new List<Shape>();

		var rX = UnityEngine.Random.Range(0f, 1f) > 0.5f ? 1 : 0;
		var rY = UnityEngine.Random.Range(0f, 1f) > 0.5f ? 1 : 0;

		for (var x = 0; x < _width; x++)
		{
			for (var y = 0; y < _height; y++)
			{
				var position = _triangleOrientation == TriangleOrientation.Horizontal
					? new Vector3((x * _nodeDistance * Mathf.Sqrt(3f)) / 2f, y * _nodeDistance, 0)
					: new Vector3(x * _nodeDistance, (y * _nodeDistance * Mathf.Sqrt(3f)) / 2f, 0);

				if (_triangleOrientation == TriangleOrientation.Vertical && (y + rY) % 2 == 0)
				{
					position += Vector3.right * _nodeDistance / 2f;

					if (x == _width - 1)
					{
						continue;
					}
				}

				if (_triangleOrientation == TriangleOrientation.Horizontal && (x + rX) % 2 == 0)
				{
					position += Vector3.up * _nodeDistance / 2f;

					if (y == _height - 1)
					{
						continue;
					}
				}

				InstantiateNode(position);

				if (position.x < minX)
				{
					minX = position.x;
				}

				if (position.x > maxX)
				{
					maxX = position.x;
				}

				if (position.y < minY)
				{
					minY = position.y;
				}

				if (position.y > maxY)
				{
					maxY = position.y;
				}
			}
		}

		Size = new Vector2(maxX - minX, maxY - minY);

		transform.position -= new Vector3(((Size.x) / 2f), ((Size.y) / 2f));

		GamePanel.Instance.Initialize(_level);

		SaveConfiguration();
		SaveGrid();

#if UNITY_WEBGL
		yield return StartCoroutine(ComputeShapesAndObjectives(false));
#else
		await ComputeShapesAndObjectives(false);
#endif
		
		PostInitialize();
	}

	public void Initialize(string currentGrid, bool custom)
	{
		_completed = false;
		CustomGrid = custom;

		var splitString = currentGrid.Split(';');

		// Level

		_level = !string.IsNullOrEmpty(splitString[0])
			? int.Parse(splitString[0], CultureInfo.InvariantCulture)
			: (int?)null;

		// Width and height

		_width = int.Parse(splitString[1], CultureInfo.InvariantCulture);
		_height = int.Parse(splitString[2], CultureInfo.InvariantCulture);

		// Colors

		_colorPalette = SettingsPanel.Instance.SelectedColorPalette;

		NodeColorMap = new Dictionary<NodeColor, Color>();

		for (var i = 0; i < _nodeColors.Length; i++)
		{
			NodeColorMap.Add(_nodeColors[i], _colorPalette[i]);
		}

		var colorSplitString = splitString[3].Split(':');

		_colors = colorSplitString.Length;

		ColorOptions = new NodeColor[_colors];

		for (var i = 0; i < colorSplitString.Length; i++)
		{
			ColorOptions[i] = (NodeColor)int.Parse(colorSplitString[i], CultureInfo.InvariantCulture);
		}

		for (var i = 0; i < ColorOptions.Length; i++)
		{
			var j = UnityEngine.Random.Range(i, ColorOptions.Length);

			var temp = ColorOptions[i];
			ColorOptions[i] = ColorOptions[j];
			ColorOptions[j] = temp;
		}

		// Objective ratio

		_objectiveRatio = float.Parse(splitString[4], CultureInfo.InvariantCulture);

		// Nodes

		Nodes = new List<Node>();

		var nodeSplitString = splitString[5].Split(':');

		var minX = float.MaxValue;
		var maxX = float.MinValue;
		var minY = float.MaxValue;
		var maxY = float.MinValue;

		for (var i = 0; i < nodeSplitString.Length; i++)
		{
			var nodeInfoSplitString = nodeSplitString[i].Split('_');

			var position = new Vector3(
				float.Parse(nodeInfoSplitString[0], CultureInfo.InvariantCulture),
				float.Parse(nodeInfoSplitString[1], CultureInfo.InvariantCulture)
			);

			InstantiateNode(position, (NodeColor)int.Parse(nodeInfoSplitString[2], CultureInfo.InvariantCulture));

			if (position.x < minX)
			{
				minX = position.x;
			}

			if (position.x > maxX)
			{
				maxX = position.x;
			}

			if (position.y < minY)
			{
				minY = position.y;
			}

			if (position.y > maxY)
			{
				maxY = position.y;
			}
		}

		Size = new Vector2(maxX - minX, maxY - minY);

		// Objectives (done before shapes so that AddShape() below knows to increment the objective list)

		GamePanel.Instance.Initialize(_level);

		if (!string.IsNullOrEmpty(splitString[7]) && splitString[7][4] != '0')
		{
			Objectives = new Dictionary<ShapeKind, Objective>();

			var objectiveSplitString = splitString[7].Split(':');

			for (var i = 0; i < objectiveSplitString.Length; i++)
			{
				var objectiveInfoSplitString = objectiveSplitString[i].Split('_');

				var newObjective = new Objective
				{
					ShapeKind = (ShapeKind)int.Parse(objectiveInfoSplitString[0], CultureInfo.InvariantCulture),
					Count = int.Parse(objectiveInfoSplitString[1], CultureInfo.InvariantCulture),
					Target = int.Parse(objectiveInfoSplitString[2], CultureInfo.InvariantCulture)
				};

				Objectives.Add(newObjective.ShapeKind, newObjective);
			}

			GamePanel.Instance.InitializeGridFeedback(_shapeKinds, NodeColorMap.Values.ToArray());
			GamePanel.Instance.InitializeObjectives();
			CheckObjectives(true);
		}
		else
		{
#if UNITY_WEBGL
			StartCoroutine(ComputeShapesAndObjectives(true));
#else
			_ = ComputeShapesAndObjectives(true);
#endif
		}

		// Shapes

		Shapes = new List<Shape>();

		if (!string.IsNullOrEmpty(splitString[6]))
		{
			var shapeSplitString = splitString[6].Split(':');

			for (var i = 0; i < shapeSplitString.Length; i++)
			{
				var shapeInfoSplitString = shapeSplitString[i].Split('_');
				var color = (NodeColor)int.Parse(shapeInfoSplitString[0], CultureInfo.InvariantCulture);

				var vertexSplitString = shapeInfoSplitString[1].Split('+');
				var vertices = new Vector3[vertexSplitString.Length];

				for (var j = 0; j < vertexSplitString.Length; j++)
				{
					var vertexInfoSplitString = vertexSplitString[j].Split('*');

					vertices[j] = new Vector3(
						float.Parse(vertexInfoSplitString[0], CultureInfo.InvariantCulture),
						float.Parse(vertexInfoSplitString[1], CultureInfo.InvariantCulture)
					);
				}

				AddShape(vertices, color, (ShapeKind)vertices.Length, true);
			}
		}

		// Finalizing

		PostInitialize();
	}

	[Button]
	public void ComputeShapesAndObjective()
	{
		if (Shapes != null)
		{
			foreach (var shape in Shapes)
			{
				Destroy(shape.gameObject);
			}

			Shapes.Clear();
		}

		_ = ComputeShapesAndObjectives(true);
	}

	private void PostInitialize()
	{
		_player.InputType = (InputType)SettingsPanel.Instance.InputTypeSelection;

		SaveConfiguration();
		SaveProgress();
		SaveGrid();

		/*Debug.Log(
			$"Total triangles : {_validShapes.Count(x => x.Nodes.Count == (int)ShapeKind.Triangle)}\n" +
			$"Total diamonds : {_validShapes.Count(x => x.Nodes.Count == (int)ShapeKind.Diamond)}\n" +
			$"Total hexagons : {_validShapes.Count(x => x.Nodes.Count == (int)ShapeKind.Hexagon)}\n" +
			$"Total : {_validShapes.Count}\n" +
			$"-----------------------------\n" +
			$"Max total shapes : {Mathf.CeilToInt(_validShapes.Count * (_objectiveRatio / 100f))}\n" +
			$"Total objectives : {Objectives[ShapeKind.Triangle].Target + Objectives[ShapeKind.Diamond].Target + Objectives[ShapeKind.Hexagon].Target}\n" +
			$"-----------------------------\n" +
			$"Objective ratio : {_objectiveRatio}\n" +
			$"Triangle ratio : {(float)Objectives[ShapeKind.Triangle].Target / (float)_validShapes.Count(x => x.Nodes.Count == (int)ShapeKind.Triangle) * 100f}\n" +
			$"Diamond ratio : {(float)Objectives[ShapeKind.Diamond].Target / (float)_validShapes.Count(x => x.Nodes.Count == (int)ShapeKind.Diamond) * 100f}\n" +
			$"Hexagons ratio : {(float)Objectives[ShapeKind.Hexagon].Target / (float)_validShapes.Count(x => x.Nodes.Count == (int)ShapeKind.Hexagon) * 100f}\n" +
			$"Total ratio : {(float)(Objectives[ShapeKind.Triangle].Target + Objectives[ShapeKind.Diamond].Target + Objectives[ShapeKind.Hexagon].Target) / (float)_validShapes.Count * 100f}");*/
	}

#if UNITY_WEBGL
	private IEnumerator ComputeShapesAndObjectives(bool fromSave)
#else
	private async Task ComputeShapesAndObjectives(bool fromSave)
#endif
	{
		var startTime = DateTime.Now;

		Objectives = new Dictionary<ShapeKind, Objective>();

		foreach (var shapeKind in _shapeKinds)
		{
			var placeolderObjective = new Objective
			{
				ShapeKind = shapeKind,
				Count = 0,
				Target = 0
			};

			Objectives.Add(shapeKind, placeolderObjective);
		}

		if (_level.HasValue && _level == 1)
		{
			Objectives[ShapeKind.Triangle].Target = 1;
		}
		else
		{
			_computeShapeCancellationTokenSource = new CancellationTokenSource();
	
#if UNITY_WEBGL

			_computationCount = 0;
			_cachedComputationCount = 0;
			_computationTimer = _computationInterval;

			StartCoroutine(ComputeShapes(_computeShapeCancellationTokenSource.Token));

			yield return new WaitUntil(() =>
			{
				if (_computationTimer > 0f)
				{
					return false;
				}
				else
				{
					//Debug.Log(_cachedComputationCount - _computationCount);

					if (_cachedComputationCount - _computationCount == 0)
					{
						return true;
					}

					_computationTimer = _computationInterval;

					_cachedComputationCount = _computationCount;

					return false;
				}
			});
#else
			await ComputeShapes(_computeShapeCancellationTokenSource.Token);
#endif
			
#if UNITY_EDITOR

			foreach (var shape in _validShapes)
			{
				AddShape(shape.Nodes.Select(x => x.transform.position).ToArray(), shape.Nodes[0].Color, (ShapeKind)shape.Nodes.Count, true, true);
				//AddShape(shape.Nodes.Select(x => x.transform.position).ToArray(), shape.Nodes[0].Color, (ShapeKind)shape.Nodes.Count, true, false);
			}

#endif

			var extraLevels = _level.HasValue
				? _level.Value - _progression.Length > 0
					? _level.Value - _progression.Length
					: 0
				: 0;

			var totalShapes = _validShapes.Count;
			var maxTotalShapes = _level == 1
				? 1
				: Math.Min(
					Mathf.CeilToInt(_validShapes.Count * (_objectiveRatio / 100f)) + extraLevels,
					totalShapes);

			var maxTotalShapeMap = new Dictionary<ShapeKind, int>();
			var totalShapeMap = new Dictionary<ShapeKind, int>();

			foreach (var shapeKind in _shapeKinds)
			{
				var totalShapeOfKind = _validShapes.Count(x => x.Nodes.Count == (int)shapeKind);

				totalShapeMap.Add(shapeKind, totalShapeOfKind);

				maxTotalShapeMap.Add(
					shapeKind,
					Math.Min(
						Mathf.CeilToInt(totalShapeOfKind * (_objectiveRatio / 100f)) + extraLevels,
						totalShapeOfKind));
			}

			while (Objectives[ShapeKind.Triangle].Target + Objectives[ShapeKind.Diamond].Target + Objectives[ShapeKind.Hexagon].Target < maxTotalShapes)
			{
				var continues = 0;

				foreach (var shapeKind in _shapeKinds)
				{
					if (Objectives[shapeKind].Target >= maxTotalShapeMap[shapeKind])
					{
						continues++;

						continue;
					}

					Objectives[shapeKind].Target++;

					if (shapeKind == ShapeKind.Diamond
						&& Objectives[ShapeKind.Triangle].Target < maxTotalShapeMap[ShapeKind.Triangle] - 1
						&& Objectives[ShapeKind.Triangle].Target < totalShapeMap[ShapeKind.Triangle] - 1)
					{
						Objectives[ShapeKind.Triangle].Target += 2;
					}
					else if (shapeKind == ShapeKind.Hexagon
						&& Objectives[ShapeKind.Triangle].Target < maxTotalShapeMap[ShapeKind.Triangle] - 2
						&& Objectives[ShapeKind.Triangle].Target < totalShapeMap[ShapeKind.Triangle] - 2)
					{
						Objectives[ShapeKind.Triangle].Target += 3;
					}

					if (Objectives[ShapeKind.Triangle].Target + Objectives[ShapeKind.Diamond].Target + Objectives[ShapeKind.Hexagon].Target >= maxTotalShapes)
					{
						//Debug.Log("Every objective distributed between available shapes. Success !");

						break;
					}
				}

				if (continues == 3)
				{
					break;
				}
			}

			//Debug.Log($"Computed in {(DateTime.Now - startTime).TotalSeconds} seconds");
		}
		
		GamePanel.Instance.InitializeGridFeedback(_shapeKinds, NodeColorMap.Values.ToArray());
		GamePanel.Instance.InitializeObjectives();
		CheckObjectives(fromSave);

		SaveProgress();
		SaveGrid();
	}

	private void SaveConfiguration()
	{
		var colorString = string.Empty;

		for (var i = 0; i < ColorOptions.Length; i++)
		{
			colorString += $"{(int)ColorOptions[i]}{(i < ColorOptions.Length - 1 ? ":" : string.Empty)}";
		}

		// ; : _ + *

		// Configuration

		_configurationString =
			$"{_level};" +
			$"{_width};" +
			$"{_height};" +
			$"{colorString};" +
			$"{_objectiveRatio};";

		// Nodes

		for (var i = 0; i < Nodes.Count; i++)
		{
			var nodeString = $"{Nodes[i].transform.position.x}_{Nodes[i].transform.position.y}_{(int)Nodes[i].Color}";

			_configurationString += $"{nodeString}{(i < Nodes.Count - 1 ? ":" : string.Empty)}";
		}

		// Shapes

		/*if (_customGrid)
		{
			GamePanel.Instance.CurrentCustomGrid = savedString.Replace(",", ".");
		}
		else
		{
			GamePanel.Instance.CurrentProgressionGrid = savedString.Replace(",", ".");
		}*/

		//Debug.Log(savedString.Replace(",", "."));
	}

	private void SaveProgress()
	{
		_progressString = string.Empty;

		for (var i = 0; i < Shapes.Count; i++)
		{
			var shapeString = $"{(int)Shapes[i].Color}_";

			for (var j = 0; j < Shapes[i].Vertices.Count; j++)
			{
				var verticesString = $"{Shapes[i].Vertices[j].x}*{Shapes[i].Vertices[j].y}";

				shapeString += $"{verticesString}{(j < Shapes[i].Vertices.Count - 1 ? "+" : string.Empty)}";
			}

			_progressString += $"{shapeString}{(i < Shapes.Count - 1 ? ":" : string.Empty)}";
		}

		_progressString += ";";

		// Objectives

		var objectives = Objectives.Values/*.Where(x => x != null)*/.ToArray();

		for (var i = 0; i < objectives.Length; i++)
		{
			var objectiveString = $"{(int)objectives[i].ShapeKind}_{objectives[i].Count}_{objectives[i].Target}";

			_progressString += $"{objectiveString}{(i < objectives.Length - 1 ? ":" : string.Empty)}";
		}
	}

	private void SaveGrid()
	{
		var configurationString = !string.IsNullOrEmpty(_configurationString) ? _configurationString : ";;;;;";
		var progressString = !string.IsNullOrEmpty(_progressString) ? _progressString : ";";

		var savedString = $"{configurationString};{progressString}";

		if (CustomGrid)
		{
			GamePanel.Instance.CurrentCustomGrid = savedString.Replace(",", ".");
		}
		else
		{
			GamePanel.Instance.CurrentProgressionGrid = savedString.Replace(",", ".");
		}
	}

	private void InstantiateNode(Vector3 position, NodeColor? color = null)
	{
		var newNode = Instantiate(_nodePrefab, transform).GetComponent<Node>();
		newNode.transform.position = position;

		newNode.Initialize(this, _player.InputType, color);

		Nodes.Add(newNode);
	}

	public void AddShape(Vector3[] simplifiedShape, NodeColor color, ShapeKind shapeKind/*, ??? size*/, bool fromSave, bool debug = false)
	{
		var newShape = Instantiate(_shapeTemplate).GetComponent<Shape>();
		var mesh = _shapeGenerator.GenerateShape(simplifiedShape);

		newShape.transform.SetParent(transform, true);
		newShape.Initialize(this, mesh, color, shapeKind, /*size, */Shapes.Count, fromSave, debug);
		//newShape.Initialize(this, mesh, color, shapeKind, /*size, */Shapes.Count, fromSave, false);

		if (!debug)
		{
			Shapes.Add(newShape);
		}

		if (!fromSave)
		{
			SaveProgress();
			SaveGrid();
		}
	}

	public void BringToFront(Shape topShape)
	{
		if (Shapes[Shapes.Count - 1] == topShape)
		{
			return;
		}

		Shapes.Remove(topShape);
		Shapes.Add(topShape);

		for (var i = 0; i < Shapes.Count; i++)
		{
			var shape = Shapes[i];

			shape.transform.position = new Vector3(shape.transform.position.x, shape.transform.position.y, -i * 0.01f);
		}
	}

	public void CheckObjectives(ShapeKind shapeKind)
	{
		GamePanel.Instance.UpdateObjectiveTracker(shapeKind);

		CheckObjectives();
	}

	public void CheckObjectives(bool fromSave = false)
	{
		if (Objectives == null || _completed)
		{
			return;
		}

		var check = true;
		var totalObjectives = 0;

		foreach (var objective in Objectives.Values)
		{
			if (objective == null)
			{
				continue;
			}

			totalObjectives += objective.Target;

			if (objective.Count < objective.Target)
			{
				check = false;

				break;
			}
		}

		if (check && totalObjectives > 0)
		{
			if (!CustomGrid)
			{
				if (!fromSave)
				{
					MenuPanel.Instance.ProgressionIndex++;
				}

				GamePanel.Instance.ToggleSubPanel(GamePanel.Instance.NextButton, true, true);
			}
			else
			{
				GamePanel.Instance.ToggleSubPanel(GamePanel.Instance.NextCustomButton, true, true);
			}

			GamePanel.Instance.ToggleSubPanel(GamePanel.Instance.ResetButton, false, true);

			GamePanel.Instance.ShowCompletedGridFeedback();

			_completed = true;
		}
	}

	public Vector3[] SimplifyShape(List<Node> shape)
	{
		var vertices = new List<Vector3>();
		var direction = Vector3.zero;

		for (var i = 0; i < shape.Count - 1; i++)
		{
			var newDirection = (shape[i + 1].transform.position - shape[i].transform.position).normalized;

			if (direction != newDirection)
			{
				vertices.Add(shape[i].transform.position);
				direction = newDirection;
			}
		}

		return vertices.ToArray();
	}

	public bool ValidateShape(Vector3[] vertices, int sides, Vector3? center = null)
	{
		if (vertices.Length != sides)
		{
			return false;
		}

		var validKind = false;

		foreach (var shapeKind in _shapeKinds)
		{
			if (vertices.Length == (int)shapeKind)
			{
				validKind = true;
				break;
			}
		}

		if (!validKind)
		{
			return false;
		}

		var isDiamond = sides == 4;

		if (!center.HasValue && !isDiamond)
		{
			center = FindShapeCenter(vertices);
		}

		var distance = Vector3.Distance(vertices[vertices.Length - 1], vertices[0]);
		var distanceToCenter = isDiamond ? 0f : Vector3.Distance(vertices[vertices.Length - 1], center.Value);

		for (var i = 0; i < vertices.Length - 1; i++)
		{
			var newDistance = Vector3.Distance(vertices[i], vertices[i + 1]);
			var newDistanceToCenter = isDiamond ? 0f : Vector3.Distance(center.Value, vertices[i + 1]);

			if (Mathf.Abs(distance - newDistance) > 0.1f || (!isDiamond && Mathf.Abs(distanceToCenter - newDistanceToCenter) > 0.1f))
			{
				return false;
			}

			if (isDiamond)
			{
				var diagonalA = Vector3.Distance(vertices[0], vertices[2]);
				var diagonalB = Vector3.Distance(vertices[1], vertices[3]);
				var diagonalRatio = diagonalA < diagonalB ? diagonalA / diagonalB : diagonalB / diagonalA;

				if (diagonalRatio < 0.55f || diagonalRatio > 0.6f)
				{
					return false;
				}
			}
		}

		return true;
	}

	public Vector3 FindShapeCenter(Vector3[] vertices)
	{
		var center = Vector3.zero;

		foreach (var vertex in vertices)
		{
			center += vertex;
		}

		return center / vertices.Length;
	}

#if UNITY_WEBGL
	private IEnumerator ComputeShapes(CancellationToken cancellationToken)
#else
	private async Task ComputeShapes(CancellationToken cancellationToken)
#endif
	{
		_validShapes = new List<SimpleShape>();

#if UNITY_WEBGL
#else
		await Task.Run(async () =>
		{
#endif
			foreach (var color in ColorOptions)
			{
				if (cancellationToken.IsCancellationRequested)
				{
#if UNITY_WEBGL
					yield break;
#else
					return;
#endif
				}

				foreach (var shapeKind in _shapeKinds)
				{
					// TEMP !!!
					/*if(shapeKind != ShapeKind.Triangle)
					{
						continue;
					}*/
					//

					if (cancellationToken.IsCancellationRequested)
					{
#if UNITY_WEBGL
						yield break;
#else
						return;
#endif
					}

					var nodes = Nodes.Where(x => !x.Grey && x.Color == color).ToArray();
					var sides = (int)shapeKind;
					var nodeData = new Node[sides];

#if UNITY_WEBGL
					StartCoroutine(ComputeShapes(nodes, nodeData, 0, nodes.Length - 1, 0, sides, cancellationToken));

					yield return null;
#else
					_ = ComputeShapes(nodes, nodeData, 0, nodes.Length - 1, 0, sides, cancellationToken);

					await Task.Yield();
#endif
				}
			}
#if UNITY_WEBGL
#else
		});
#endif

		if (cancellationToken.IsCancellationRequested)
		{
#if UNITY_WEBGL
			yield return null;
#else
			return;
#endif
		}
	}

#if UNITY_WEBGL
	protected IEnumerator ComputeShapes(Node[] nodes, Node[] nodeData, int start, int end, int index, int sides, CancellationToken cancellationToken)
#else
	private async Task ComputeShapes(Node[] nodes, Node[] nodeData, int start, int end, int index, int sides, CancellationToken cancellationToken)
#endif
	{
		if (index == sides)
		{
			var shape = new SimpleShape(nodeData);
			var vertices = new Vector3[shape.Nodes.Count];

			for (var i = 0; i < vertices.Length; i++)
			{
				vertices[i] = shape.Nodes[i].Position;
			}

			var center = FindShapeCenter(vertices);

			if(sides != 3)
			{
				vertices = vertices.OrderBy(x => { return Vector3.SignedAngle(center - x, center + Vector3.right, Vector3.forward); }).ToArray();
			}
			
			var isValid = ValidateShape(vertices, sides, center);

			if (isValid)
			{
				_validShapes.Add(shape);
			}

#if UNITY_WEBGL
			_computationCount++;
#endif
		}
		else
		{
			for (var i = start; i <= end && end - i + 1 >= sides - index; i++)
			{
				if (cancellationToken.IsCancellationRequested)
				{
#if UNITY_WEBGL
					yield break;
#else
					return;
#endif
				}

				nodeData[index] = nodes[i];

#if UNITY_WEBGL
				yield return StartCoroutine(ComputeShapes(nodes, nodeData, i + 1, end, index + 1, sides, cancellationToken));
#else
				_ = ComputeShapes(nodes, nodeData, i + 1,  end, index + 1, sides, cancellationToken);
#endif
			}
		}
	}
	
	public void CleanUp(bool reset, bool quit)
	{
		if (_computeShapeCancellationTokenSource != null)
		{
			_computeShapeCancellationTokenSource.Cancel();
		}
		
		if(quit)
		{
			return;
		}

		_completed = false;
		_configurationString = null;
		_progressString = null;

		if (!reset)
		{
			_level = null;
		}
		
		if (_validShapes != null)
		{
			_validShapes.Clear();
		}
		
		if (Objectives != null)
		{
			Objectives.Clear();
		}
		
		if (Nodes != null)
		{
			foreach (var node in Nodes)
			{
				node.Disappear();
			}

			Nodes.Clear();
		}

#if UNITY_EDITOR
		var debugShapes = GetComponentsInChildren<Shape>(true);

		foreach (var debugShape in debugShapes)
		{
			Destroy(debugShape.gameObject);
		}
#endif

		if (Shapes != null)
		{
			foreach (var shape in Shapes)
			{
				shape.Disappear();
			}

			Shapes.Clear();
		}

		if (NodeColorMap != null)
		{
			NodeColorMap.Clear();
		}

		ColorOptions = null;
	}

	private void OnDestroy()
	{
		CleanUp(false, true);
	}
}
