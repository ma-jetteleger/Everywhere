using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;

public class GridFeedbackShape : MonoBehaviour
{
	[SerializeField] private CanvasGroup _canvasGroup = null;
	[SerializeField] private GameObject _triangle = null;
	[SerializeField] private GameObject _diamond = null;
	[SerializeField] private GameObject _hexagon = null;
	[SerializeField] private Image _triangleImage = null;
	[SerializeField] private Image _diamondImage = null;
	[SerializeField] private Image _hexagonImage = null;
	[SerializeField] private float[] _possibleScales = null;
	[SerializeField] private float[] _possibleRotations = null;
	[SerializeField] private float _gridFeedbackMaxStartTime = 0f;
	[SerializeField] private Vector2 _moveTimeRange = Vector2.zero;
	[SerializeField] private AnimationCurve[] _moveCurves = null;

	private float _bottomY;
	private float _topY;
	private Tweener _moveTween;
	
	public void Initialize(ShapeKind[] shapeKinds, Color[] colors, float bottomY, float topY)
	{
		_bottomY = bottomY;
		_topY = topY;

		var color = colors[Random.Range(0, colors.Length)];

		var shapeKindIndex = Random.Range(0f, 1f);
		var shapeKind = (ShapeKind)0;

		if (shapeKindIndex > 0.85f)
		{
			shapeKind = ShapeKind.Hexagon;
			_hexagonImage.color = color;
		}
		else if(shapeKindIndex > 0.5f)
		{
			shapeKind = ShapeKind.Diamond;
			_diamondImage.color = color;
		}
		else
		{
			shapeKind = ShapeKind.Triangle;
			_triangleImage.color = color;
		}

		_triangle.SetActive(shapeKind == ShapeKind.Triangle);
		_diamond.SetActive(shapeKind == ShapeKind.Diamond);
		_hexagon.SetActive(shapeKind == ShapeKind.Hexagon);

		var hFlip = Random.Range(0f, 1f) > 0.5f;
		var vFlip = Random.Range(0f, 1f) > 0.5f;

		transform.localScale = new Vector3(hFlip ? -1f : 1f, vFlip ? -1f : 1f, 1f) * _possibleScales[Random.Range(0, _possibleScales.Length)];
		transform.eulerAngles = new Vector3(0f, 0f, _possibleRotations[Random.Range(0, _possibleRotations.Length)]);
		
		Hide();
	}

	public void Show()
	{
		CancelInvoke();

		if (_moveTween != null)
		{
			_moveTween.Kill();
			_moveTween = null;
		}

		transform.position = new Vector3(transform.position.x, _bottomY, 0f);

		Invoke(nameof(Move), Random.Range(0, _gridFeedbackMaxStartTime));
	}

	public void Hide()
	{
		CancelInvoke();

		if (_moveTween != null)
		{
			_moveTween.Kill();
			_moveTween = null;
		}
		
		transform.position = new Vector3(transform.position.x, _bottomY, 0f);

		_canvasGroup.alpha = 0f;
	}

	private void Move()
	{
		_canvasGroup.alpha = 1f;

		_moveTween = transform.DOMoveY(_topY, Random.Range(_moveTimeRange.x, _moveTimeRange.y)).SetEase(_moveCurves[Random.Range(0, _moveCurves.Length)]).OnComplete(() =>
		{
			_canvasGroup.alpha = 0f;
		});
	}
}
