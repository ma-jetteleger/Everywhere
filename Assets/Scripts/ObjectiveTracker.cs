using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class ObjectiveTracker : MonoBehaviour
{
	[SerializeField] private Image _objectiveIcon = null;
	[SerializeField] private TextMeshProUGUI _objectiveText = null;
	[SerializeField] private Image _outline = null;
	[SerializeField] private Image _overlay = null;
	[SerializeField] private float _overlayAlphaRatio = 0f;
	[SerializeField] private float _completedShapeFeedbackTime = 0f;
	[SerializeField] private AnimationCurve _completedShapeOutlineFeedbackCurve = null;
	[SerializeField] private AnimationCurve _completedShapeOverlayFeedbackCurve = null;
	[SerializeField] private Sprite _triangleSprite = null;
	[SerializeField] private Sprite _diamondSprite = null;
	[SerializeField] private Sprite _hexagonSprite = null;
	
	public Objective Objective { get; set; }
	public RectTransform RectTransform { get; set; }

	private bool _complete;

	public void Initialize(Objective objective)
	{
		Objective = objective;
		RectTransform = GetComponent<RectTransform>();

		switch (Objective.ShapeKind)
		{
			case ShapeKind.Triangle:
				_objectiveIcon.sprite = _triangleSprite;
				break;
			case ShapeKind.Diamond:
				_objectiveIcon.sprite = _diamondSprite;
				break;
			case ShapeKind.Hexagon:
				_objectiveIcon.sprite = _hexagonSprite;
				break;
		}

		/*if (Objective.ColorObjective.HasValue)
		{
			_objectiveIcon.color = _grid.NodeColorMap[Objective.ColorObjective.Value];
		}*/

		/*if (Objective.SizeObjective.HasValue)
		{
			//
		}*/

		_objectiveText.text = $"{Objective.Count} / {Objective.Target}";

		_complete = Objective.Count >= Objective.Target;

		_outline.color = _complete ? Color.white : Color.clear;
		_overlay.color = Color.clear;
	}

	public void UpdateCounter()
	{
		_objectiveText.text = $"{Objective.Count} / {Objective.Target}";
	}

	public void ShowCompletedShapeFeedback(Color shapeColor)
	{
		var outlineColor = _complete ? Color.white : new Color(1f, 1f, 1f, 0f);
		var overlayColor = shapeColor;
		overlayColor.a = 0f;
		
		DOTween.To(() => 0f, x =>
		{
			if(!_complete)
			{
				if(!(x >= 0.5f && Objective.Count >= Objective.Target))
				{
					var outlineValue = _completedShapeOutlineFeedbackCurve.Evaluate(x);
					outlineColor.a = outlineValue;

					_outline.color = outlineColor;
				}
				else
				{
					_outline.color = Color.white;
				}
			}
			
			var overlayValue = _completedShapeOverlayFeedbackCurve.Evaluate(x);
			overlayColor.a = overlayValue * _overlayAlphaRatio;

			_overlay.color = overlayColor;
		},
		1f,
		_completedShapeFeedbackTime).OnComplete(() =>
		{
			if(!_complete && Objective.Count >= Objective.Target)
			{
				_complete = true;
			}
			
			_outline.color = _complete ? Color.white : Color.clear;
			_overlay.color = Color.clear;
		});
	}
}
