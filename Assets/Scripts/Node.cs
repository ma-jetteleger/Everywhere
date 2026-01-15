using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using NaughtyAttributes;

public enum NodeColor
{
	Blue,
	Orange,
	Green,
	Purple
}

public enum NodeState
{
	Normal,
	Hovered,
	ShapeStart,
	HoveredShapeStart
}

[SelectionBase]
public class Node : MonoBehaviour
{
	public const string kArc1 = "_Arc1";

	[SerializeField] private SpriteRenderer _back = null;
	[SerializeField] private SpriteRenderer _heldFeedback = null;
	[SerializeField] private SpriteRenderer _heldFeedbackBall = null;
	[SerializeField] private SpriteRenderer _front = null;
	[SerializeField] private SpriteRenderer _center = null;
	[SerializeField] private CircleCollider2D _collider = null;
	[SerializeField] private float _dragColliderRadius = 0f;
	[SerializeField] private float _clickColliderRadius = 0f;
	[SerializeField] private float _frontHoveredScale = 0f;
	[SerializeField] private float _backHoveredScale = 0f;
	[SerializeField] private float _frontStartScale = 0f;
	[SerializeField] private float _backStartScale = 0f;
	[SerializeField] private float _scaleUpTime = 0f;
	[SerializeField] private float _scaleDownTime = 0f;
	[SerializeField] private AnimationCurve _scaleCurve = null;
	[SerializeField] private float _punchTime = 0f;
	[SerializeField] private float _colorAppearDelay = 0f;
	[SerializeField] private float _appearTime = 0f;
	[SerializeField] private float _disappearTime = 0f;
	[SerializeField] private AnimationCurve _appearCurve = null;

	public Vector3 Position { get; set; }
	public bool Grey { get; set; }

	private Tweener[] _scalings;
	private Tweener[] _punches;
	private Tweener[] _fadings;
	private int _normalBackOrder;
	private int _normalHoveredOrder;
	private int _normalFrontOrder;

	public NodeColor Color
	{
		get
		{
			return _nodeColor;
		}
		set
		{
			_nodeColor = value;

			_front.color = _grid.NodeColorMap[_nodeColor];
		}
	}

	public NodeState State
	{
		set
		{
			KillTweeners();

			switch (value)
			{
				case NodeState.Normal:

					_scalings[0] = _back.transform.DOScale(Vector3.one / _backHoveredScale, _scaleDownTime).SetEase(_scaleCurve);
					_scalings[1] = _heldFeedback.transform.DOScale(Vector3.one / _backHoveredScale, _scaleDownTime).SetEase(_scaleCurve);
					_scalings[2] = _front.transform.DOScale(Vector3.one / _frontHoveredScale, _scaleDownTime).SetEase(_scaleCurve);
					_scalings[3] = _center.transform.DOScale(Vector3.zero, _scaleDownTime).SetEase(_scaleCurve);

					_back.sortingOrder = _normalBackOrder;
					_heldFeedback.sortingOrder = _normalHoveredOrder;
					_heldFeedbackBall.sortingOrder = _normalHoveredOrder;
					_front.sortingOrder = _normalFrontOrder;

					break;

				case NodeState.Hovered:

					_scalings[0] = _back.transform.DOScale(Vector3.one, _scaleUpTime).SetEase(_scaleCurve);
					_scalings[1] = _heldFeedback.transform.DOScale(Vector3.one, _scaleUpTime).SetEase(_scaleCurve);
					_scalings[2] = _front.transform.DOScale(Vector3.one, _scaleUpTime).SetEase(_scaleCurve);
					_scalings[3] = _center.transform.DOScale(Vector3.zero, _scaleDownTime).SetEase(_scaleCurve);

					_back.sortingOrder = _normalBackOrder + 3;
					_heldFeedback.sortingOrder = _normalHoveredOrder + 3;
					_heldFeedbackBall.sortingOrder = _normalHoveredOrder + 3;
					_front.sortingOrder = _normalFrontOrder + 3;

					break;

				case NodeState.ShapeStart:

					_scalings[0] = _back.transform.DOScale(Vector3.one / _backStartScale, _scaleDownTime).SetEase(_scaleCurve);
					_scalings[1] = _heldFeedback.transform.DOScale(Vector3.one / _backStartScale, _scaleDownTime).SetEase(_scaleCurve);
					_scalings[2] = _front.transform.DOScale(Vector3.one / _frontStartScale, _scaleDownTime).SetEase(_scaleCurve);
					_scalings[3] = _center.transform.DOScale(Vector3.one, _scaleUpTime).SetEase(_scaleCurve);

					_back.sortingOrder = _normalBackOrder + 6;
					_heldFeedback.sortingOrder = _normalHoveredOrder + 6;
					_heldFeedbackBall.sortingOrder = _normalHoveredOrder + 6;
					_front.sortingOrder = _normalFrontOrder + 6;

					break;

				case NodeState.HoveredShapeStart:

					_scalings[0] = _back.transform.DOScale(Vector3.one, _scaleUpTime).SetEase(_scaleCurve);
					_scalings[1] = _heldFeedback.transform.DOScale(Vector3.one, _scaleUpTime).SetEase(_scaleCurve);
					_scalings[2] = _front.transform.DOScale(Vector3.one, _scaleUpTime).SetEase(_scaleCurve);
					_scalings[3] = _center.transform.DOScale(Vector3.one, _scaleUpTime).SetEase(_scaleCurve);

					_back.sortingOrder = _normalBackOrder + 6;
					_heldFeedback.sortingOrder = _normalHoveredOrder + 6;
					_heldFeedbackBall.sortingOrder = _normalHoveredOrder + 6;
					_front.sortingOrder = _normalFrontOrder + 6;

					break;
			}

			_nodeState = value;
		}
		get
		{
			return _nodeState;
		}
	}

	private TriangleGrid _grid;
	private NodeColor _nodeColor;
	private Material _heldFeedbackMaterial;
	private NodeState _nodeState;

	public void Initialize(TriangleGrid grid, InputType inputType, NodeColor? color = null)
	{
		_grid = grid;
		_heldFeedbackMaterial = _heldFeedback.sharedMaterial;

		var colorIndex = 0;

		if (color.HasValue)
		{
			for (var i = 0; i < _grid.ColorOptions.Length; i++)
			{
				if (_grid.ColorOptions[i] == color.Value)
				{
					colorIndex = i;

					break;
				}
			}

			Color = color.Value;
		}
		else
		{
			colorIndex = Random.Range(0, _grid.ColorOptions.Length);
			Color = _grid.ColorOptions[colorIndex];
		}

		gameObject.name = "Node";

		Position = transform.localPosition;

		_back.color = new Color(1f, 1f, 1f, 0f);
		_front.color = new Color(_front.color.r, _front.color.g, _front.color.b, 0f);

		_back.transform.localScale = Vector3.zero;
		_heldFeedback.transform.localScale = Vector3.zero;
		_front.transform.localScale = Vector3.zero;
		_center.transform.localScale = Vector3.zero;

		_scalings = new Tweener[4];
		_punches = new Tweener[2];
		_fadings = new Tweener[2];

		_normalBackOrder = _back.sortingOrder;
		_normalHoveredOrder = _heldFeedback.sortingOrder;
		_normalFrontOrder = _front.sortingOrder;

		UpdateCollider(inputType);

		Invoke(nameof(Appear), _colorAppearDelay * colorIndex);
	}

	[Button]
	public void MakeEverythingGrey()
	{
		foreach (var node in _grid.Nodes)
		{
			node.MakeGrey();
		}
	}

	[Button]
	public void MakeGrey()
	{
		Grey = true;

		_front.color = UnityEngine.Color.gray;
	}

	[Button]
	public void ChangeColor()
	{
		if (Grey)
		{
			Grey = false;

			Color = 0;

			return;
		}

		var color = Color;

		if ((int)color < 3)
		{
			color += 1;

			Color = color;

			return;
		}

		if ((int)color == 3)
		{
			Grey = true;

			_front.color = UnityEngine.Color.gray;
		}
	}

	[Button]
	public void ComputeGridShapesAndObjectives()
	{
		_grid.ComputeShapesAndObjective();
	}

	private void Appear()
	{
		_fadings[0] = _back.DOFade(1f, _appearTime);
		_fadings[1] = _front.DOFade(1f, _appearTime);

		_back.transform.DOScale(Vector3.one / _backHoveredScale, _appearTime).SetEase(_appearCurve);
		_heldFeedback.transform.DOScale(Vector3.one / _backHoveredScale, _appearTime).SetEase(_appearCurve);
		_front.transform.transform.DOScale(Vector3.one / _frontHoveredScale, _appearTime).SetEase(_appearCurve);
	}

	public void Disappear()
	{
		transform.SetParent(transform.parent.parent, true);

		_fadings[0] = _back.DOFade(0f, _disappearTime);
		_fadings[1] = _front.DOFade(0f, _disappearTime).OnComplete(() =>
		{
			Destroy(gameObject);
		});
	}

	public void Punch(bool firstNode)
	{
		_back.sortingOrder = _normalBackOrder + 3;
		_heldFeedback.sortingOrder = _normalHoveredOrder + 3;
		_heldFeedbackBall.sortingOrder = _normalHoveredOrder + 3;
		_front.sortingOrder = _normalFrontOrder + 3;

		_punches[0] = _back.transform.DOPunchScale(Vector3.one, _punchTime, 0);
		_punches[1] = _front.transform.DOPunchScale(Vector3.one, _punchTime, 0).OnComplete(() =>
		{
			_back.sortingOrder = _normalBackOrder;
			_heldFeedback.sortingOrder = _normalHoveredOrder;
			_heldFeedbackBall.sortingOrder = _normalHoveredOrder;
			_front.sortingOrder = _normalFrontOrder;

			if (firstNode)
			{
				State = NodeState.ShapeStart;
			}
		});
	}

	public void UpdateCollider(InputType inputType)
	{
		_collider.radius = inputType == InputType.DragAndRelease ? _dragColliderRadius : _clickColliderRadius;
	}

	public void UpdateHeldFeedback(float progress)
	{
		_heldFeedback.material.SetFloat(kArc1, 360f - (progress * 360f));

		if(!_heldFeedbackBall.enabled)
		{
			_heldFeedbackBall.enabled = true;
		}
	}

	public void ResetHeldFeedback()
	{
		_heldFeedback.material.SetFloat(kArc1, 360f);

		_heldFeedbackBall.enabled = false;
	}

	private void KillTweeners()
	{
		CancelInvoke();

		for (var i = 0; i < _scalings.Length; i++)
		{
			if (_scalings[i] == null)
			{
				continue;
			}

			_scalings[i].Kill();
			_scalings[i] = null;
		}

		for (var i = 0; i < _punches.Length; i++)
		{
			if (_punches[i] == null)
			{
				continue;
			}

			_punches[i].Kill();
			_punches[i] = null;
		}

		for (var i = 0; i < _fadings.Length; i++)
		{
			if (_fadings[i] == null)
			{
				continue;
			}

			_fadings[i].Kill();
			_fadings[i] = null;
		}
	}

	private void OnDestroy()
	{
		KillTweeners();
	}
}
