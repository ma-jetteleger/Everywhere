using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class CompletedShapeFeedback : MonoBehaviour
{
	[SerializeField] private GameObject _triangle = null;
	[SerializeField] private GameObject _diamond = null;
	[SerializeField] private GameObject _hexagon = null;
	[SerializeField] private CanvasGroup _canvasGroup = null;
	[SerializeField] private Image _triangleImage = null;
	[SerializeField] private Image _diamondImage = null;
	[SerializeField] private Image _hexagonImage = null;
	[SerializeField] private float _timeIn = 0f;
	[SerializeField] private float _timeOut = 0f;
	[SerializeField] private AnimationCurve _moveToShapeCurve = null;
	[SerializeField] private AnimationCurve _moveToTopCurve = null;
	[SerializeField] private AnimationCurve _fadeInCurve = null;
	[SerializeField] private AnimationCurve _fadeOutCurve = null;
	
	public bool Busy { get; set; }

	private Vector3 _lastBottomPosition;
	private Tweener _moveTween;
	private Tweener _fadeTween;

	public void Show(Vector3 topAnchorPosition, Vector3 bottomAnchorPosition, Vector3 shapePosition, ShapeKind shapeKind, Color color)
	{
		Busy = true;
		
		_triangle.SetActive(shapeKind == ShapeKind.Triangle);
		_diamond.SetActive(shapeKind == ShapeKind.Diamond);
		_hexagon.SetActive(shapeKind == ShapeKind.Hexagon);

		switch (shapeKind)
		{
			case ShapeKind.Triangle:
				_triangleImage.color = color;
				break;
			case ShapeKind.Diamond:
				_diamondImage.color = color;
				break;
			case ShapeKind.Hexagon:
				_hexagonImage.color = color;
				break;
		}

		_canvasGroup.alpha = 0f;
		transform.position = bottomAnchorPosition;

		_lastBottomPosition = bottomAnchorPosition;

		_moveTween = transform.DOMove(shapePosition, _timeIn).SetEase(_moveToShapeCurve).OnComplete(() =>
		{
			_moveTween = transform.DOMove(topAnchorPosition, _timeOut).SetEase(_moveToTopCurve);
		});

		_fadeTween = _canvasGroup.DOFade(1f, _timeIn).SetEase(_fadeInCurve).OnComplete(() =>
		{
			_fadeTween = _canvasGroup.DOFade(0f, _timeOut).SetEase(_fadeOutCurve).OnComplete(() =>
			{
				Busy = false;
			});
		});
	}

	public void Hide()
	{
		if (_moveTween != null)
		{
			_moveTween.Kill();
			_moveTween = null;
		}

		if (_fadeTween != null)
		{
			_fadeTween.Kill();
			_fadeTween = null;
		}

		transform.position = _lastBottomPosition;

		_canvasGroup.alpha = 0f;
	}
}
