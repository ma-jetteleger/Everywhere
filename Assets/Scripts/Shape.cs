using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using System.Linq;
using TMPro;
using UnityEngine.UI;

public enum ShapeKind
{
	Triangle = 3,
	Diamond = 4,
	Hexagon = 6
}

public class Shape : MonoBehaviour
{
	[SerializeField] private GameObject _vertexTemplate = null;
	[SerializeField] private float _lineWidthMultiplier = 0f;
	[SerializeField] private LineRenderer _releasedLineRenderer = null;
	[SerializeField] private MeshRenderer _fillRenderer = null;
	[SerializeField] private AnimationCurve _outlineUpCurve = null;
	[SerializeField] private AnimationCurve _outlineDownCurve = null;
	[SerializeField] private float _colorAppearDelay = 0f;
	[SerializeField] private float _appearTime = 0f;
	[SerializeField] private float _disappearTime = 0f;
	[SerializeField] private AnimationCurve _appearCurve = null;

	public List<Vector3> Vertices { get; set; }
	public List<Vector2> Points { get; set; }
	public NodeColor Color { get; set; }

	private SpriteRenderer[] _vertexRenderers;

	public void Initialize(TriangleGrid grid, Mesh mesh, NodeColor color, ShapeKind shapeKind,/*??? size,*/ int index, bool fromSave, bool debug)
	{
		Color = color;

		var colorIndex = 0;

		for (var i = 0; i < grid.ColorOptions.Length; i++)
		{
			if (grid.ColorOptions[i] == color)
			{
				colorIndex = i;

				break;
			}
		}
		
		Vertices = new List<Vector3>();
		Points = new List<Vector2>();
		
		var outline = new Vector3[mesh.vertexCount + 1];

		_vertexRenderers = new SpriteRenderer[mesh.vertexCount];

		for (var i = 0; i < mesh.vertexCount; i++)
		{
			outline[i] = new Vector2(mesh.vertices[i].x, mesh.vertices[i].y);

			Points.Add(new Vector2(mesh.vertices[i].x, mesh.vertices[i].y));
			Vertices.Add(new Vector2(mesh.vertices[i].x, mesh.vertices[i].y));

			var newVertex = Instantiate(_vertexTemplate, transform).transform;
			newVertex.position = mesh.vertices[i];

			newVertex.gameObject.SetActive(true);

			_vertexRenderers[i] = newVertex.GetComponent<SpriteRenderer>();
			_vertexRenderers[i].color = new Color(1f, 1f, 1f, 0f);
		}
		
		outline[outline.Length - 1] = outline[0];
		
		var collider = GetComponent<PolygonCollider2D>();
		collider.points = Points.ToArray();
		collider.enabled = true;

		var contactFilter = new ContactFilter2D();
		contactFilter.layerMask = 0;

		GetComponentInChildren<MeshFilter>().mesh = mesh;
		
		transform.position = new Vector3(transform.position.x, transform.position.y, -index * 0.01f);
		
		_releasedLineRenderer.positionCount = outline.Length;
		_releasedLineRenderer.SetPositions(outline);
		_releasedLineRenderer.sortingOrder = 0;

		gameObject.name = $"Shape({color}{shapeKind})";
		
		if(!debug)
		{
			if (fromSave)
			{
				_fillRenderer.material.color = new Color(grid.NodeColorMap[Color].r, grid.NodeColorMap[Color].g, grid.NodeColorMap[Color].b, 0f);
				_releasedLineRenderer.startColor = new Color(1f, 1f, 1f, 0f);
				_releasedLineRenderer.endColor = _releasedLineRenderer.startColor;

				_releasedLineRenderer.startWidth = _releasedLineRenderer.endWidth / _lineWidthMultiplier;
				_releasedLineRenderer.endWidth = _releasedLineRenderer.startWidth;

				Invoke(nameof(Appear), _colorAppearDelay * colorIndex);
			}
			else
			{
				if (grid.Objectives != null)
				{
					if (grid.Objectives[shapeKind] != null)
					{
						grid.Objectives[shapeKind].Count++;
					}

					grid.CheckObjectives(shapeKind/*, color*/);
				}

				var shapeColor = grid.NodeColorMap[Color];
				shapeColor.a = 0f;

				_fillRenderer.material.color = shapeColor;
				_fillRenderer.material.DOFade(1f, _appearTime).SetEase(_appearCurve);

				GamePanel.Instance.ShowCompletedShapeFeedback(grid.FindShapeCenter(mesh.vertices), shapeKind, color);

				var fromWidth = _releasedLineRenderer.endWidth;
				var midWidth = _releasedLineRenderer.endWidth * (_lineWidthMultiplier / 1.75f);
				var toWidth = _releasedLineRenderer.endWidth / _lineWidthMultiplier;

				foreach (var renderer in _vertexRenderers)
				{
					renderer.color = UnityEngine.Color.white;
				}

				var midScale = Vector3.one * (_lineWidthMultiplier / 1.75f);
				
				DOTween.To(() => 0f, x =>
				{
					var upValue = Mathf.Lerp(fromWidth, midWidth, _outlineUpCurve.Evaluate(x));

					_releasedLineRenderer.startWidth = upValue;
					_releasedLineRenderer.endWidth = upValue;

					foreach(var renderer in _vertexRenderers)
					{
						renderer.transform.localScale = Vector3.Lerp(Vector3.one, midScale, _outlineUpCurve.Evaluate(x));
					}
				}, 
				1f, _appearTime).OnComplete(() =>
				{
					DOTween.To(() => 0f, x =>
					{
						var downValue = Mathf.Lerp(midWidth, toWidth, _outlineDownCurve.Evaluate(x));

						_releasedLineRenderer.startWidth = downValue;
						_releasedLineRenderer.endWidth = downValue;

						foreach (var renderer in _vertexRenderers)
						{
							renderer.transform.localScale = Vector3.Lerp(midScale, Vector3.one, _outlineDownCurve.Evaluate(x));
						}
					},
					1f, _appearTime / 2f);
				});
			}
		}
		else
		{
			gameObject.SetActive(false);
		}
	}

	private void Appear()
	{
		for (var i = 0; i < _vertexRenderers.Length; i++)
		{
			_vertexRenderers[i].DOFade(1f, _appearTime).SetEase(_appearCurve);
		}

		_fillRenderer.material.DOFade(1f, _appearTime).SetEase(_appearCurve);
		_releasedLineRenderer.DOColor(new Color2(_releasedLineRenderer.startColor, _releasedLineRenderer.endColor), new Color2(UnityEngine.Color.white, UnityEngine.Color.white), _appearTime).SetEase(_appearCurve);
	}

	public void Disappear()
	{
		transform.SetParent(transform.parent.parent, true);

		for (var i = 0; i < _vertexRenderers.Length; i++)
		{
			_vertexRenderers[i].DOFade(0f, _disappearTime);
		}

		_fillRenderer.material.DOFade(0f, _disappearTime);
		_releasedLineRenderer.DOColor(new Color2(UnityEngine.Color.white, UnityEngine.Color.white), new Color2(new Color(1f, 1f, 1f, 0f), new Color(1f, 1f, 1f, 0f)), _disappearTime).OnComplete(() =>
		{
			Destroy(gameObject);
		});
	}
}
