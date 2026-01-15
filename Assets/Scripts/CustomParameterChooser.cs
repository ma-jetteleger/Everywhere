using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.Events;

[System.Serializable]
public class OnValueChanged : UnityEvent { };

public class CustomParameterChooser : MonoBehaviour
{
	[SerializeField] private Button _leftButton = null;
	[SerializeField] private Button _rightButton = null;
	[SerializeField] private TextMeshProUGUI _valueText = null;
	[SerializeField] private TextMeshProUGUI _label = null;
	[SerializeField] private Vector2Int _valueRange = Vector2Int.zero;
	[SerializeField] private Color _disabledTextColor = Color.white;
	[SerializeField] private OnValueChanged _onValueChanged = null;

	private Color? BaseTextColor
	{
		get
		{
			if(_baseTextColor == null)
			{
				_baseTextColor = _valueText.color;
			}

			return _baseTextColor;
		}
	}
	
	public int Value
	{
		get
		{
			return _value;
		}
		set
		{
			_value = value;

			_valueText.text = _value.ToString();
			
			_rightButton.interactable = Interactable && _value < _valueRange.y;
			_leftButton.interactable = Interactable && _value > _valueRange.x;

			_onValueChanged.Invoke();
		}
	}

	public bool Interactable
	{
		get
		{
			return _interactable;
		}
		set
		{
			_interactable = value;

			if(_interactable)
			{
				_rightButton.interactable = Value < _valueRange.y;
				_leftButton.interactable = Value > _valueRange.x;
			}
			else
			{
				_rightButton.interactable = false;
				_leftButton.interactable = false;
			}
			
			_valueText.color = _interactable ? Color.white : _disabledTextColor;
			_label.color = _interactable ? Color.white : _disabledTextColor;
		}
	}

	private int _value;
	private bool _interactable;
	private Color? _baseTextColor;

	public void UI_CycleRight()
	{
		if(Value < _valueRange.y)
		{
			Value++;
		}
	}

	public void UI_CycleLeft()
	{
		if (Value > _valueRange.x)
		{
			Value--;
		}
	}
}
