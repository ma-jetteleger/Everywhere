using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;
using DG.Tweening;

public class SettingsPanel : MonoBehaviour
{
	public static SettingsPanel Instance { get; set; }

	public const string kInputTypeSelection = "InputTypeSelection";
	public const string kColorPaletteSelection = "ColorPaletteSelection";
	
	[SerializeField] private InputType _defaultInputType = InputType.DragAndRelease;
	[SerializeField] private Color _inactiveButtonColor = Color.black;
	[SerializeField] private ColorPalette[] _colorPaletteButtons = null;
	[SerializeField] private InputTypeOption[] _inputTypeOptionButtons = null;
	[SerializeField] private CanvasGroup _clearProgressionButton = null;
	[SerializeField] private CanvasGroup _confirmClearProgressPanel = null;
	[SerializeField] private float _panelTransitionTime = 0f;

	public Color[] SelectedColorPalette => ColorPalettes[ColorPaletteSelection - 1];

	public int InputTypeSelection
	{
		get
		{
			if (_inputTypeSelection == 0)
			{
				var savedInputTypeSelection = PlayerPrefs.GetInt(kInputTypeSelection);

				if (savedInputTypeSelection != 0)
				{
					_inputTypeSelection = savedInputTypeSelection;
				}
				else
				{
					_inputTypeSelection = (int)_defaultInputType;

					PlayerPrefs.SetInt(kInputTypeSelection, _inputTypeSelection);
				}
			}

			return _inputTypeSelection;
		}
		set
		{
			_inputTypeSelection = value;

			PlayerPrefs.SetInt(kInputTypeSelection, _inputTypeSelection);
		}
	}

	public int ColorPaletteSelection
	{
		get
		{
			if (_colorPaletteSelection == 0)
			{
				var savedColorPaletteSelection = PlayerPrefs.GetInt(kColorPaletteSelection);

				if (savedColorPaletteSelection != 0)
				{
					_colorPaletteSelection = savedColorPaletteSelection;
				}
				else
				{
					_colorPaletteSelection = 1;

					PlayerPrefs.SetInt(kColorPaletteSelection, _colorPaletteSelection);
				}
			}

			return _colorPaletteSelection;
		}
		set
		{
			_colorPaletteSelection = value;

			PlayerPrefs.SetInt(kColorPaletteSelection, _colorPaletteSelection);
		}
	}

	public Color[][] ColorPalettes
	{
		get
		{
			if(_colorPalettes == null)
			{
				_colorPalettes = new Color[_colorPaletteButtons.Length][];

				for (var i = 0; i < _colorPaletteButtons.Length; i++)
				{
					var colorPalette = new Color[_colorPaletteButtons[i].ColoredNodes.Length];

					for (var j = 0; j < _colorPaletteButtons[i].ColoredNodes.Length; j++)
					{
						colorPalette[j] = _colorPaletteButtons[i].ColoredNodes[j].color;
					}

					_colorPalettes[i] = colorPalette;
				}
			}

			return _colorPalettes;
		}
	}

	public CanvasGroup Panel { get; set; }

	private int _inputTypeSelection;
	private int _colorPaletteSelection;
	private Color[][] _colorPalettes;
	private CanvasGroup _panel;
	private bool _fromCustom;

	private void Awake()
	{
		if (Instance == null)
		{
			Instance = this;
		}

		Panel = GetComponent<CanvasGroup>();
	}

	private void Start()
	{
		SelectInputTypeOption(InputTypeSelection - 1);
		SelectColorPalette(ColorPaletteSelection - 1);

		ForceLayoutRebuilding(GetComponent<RectTransform>());
	}
	
	public void Show(bool fromCustom)
	{
		ToggleSubPanel(Panel, true, true);
		ToggleSubPanel(MenuPanel.Instance.Panel, false, true);
		ToggleSubPanel(_confirmClearProgressPanel, false, false);

		_fromCustom = fromCustom;

		var currentProgressionGrid = GamePanel.Instance.CurrentProgressionGrid;
		var isContinuing = !string.IsNullOrEmpty(currentProgressionGrid) && !string.IsNullOrEmpty(currentProgressionGrid.Split(';')[6]);

		ToggleSubPanel(_clearProgressionButton, !_fromCustom && (isContinuing || MenuPanel.Instance.ProgressionIndex > 0), false);
	}
	
	public void SelectColorPalette(int colorPaletteIndex)
	{
		for (var i = 0; i < _colorPaletteButtons.Length; i++)
		{
			var active = i == colorPaletteIndex;

			_colorPaletteButtons[i].Outline.color = active ? Color.white : _inactiveButtonColor;
			_colorPaletteButtons[i].Check.enabled = active;
		}
	}

	public void SelectInputTypeOption(int inputTypeOptionIndex)
	{
		for (var i = 0; i < _inputTypeOptionButtons.Length; i++)
		{
			var active = i == inputTypeOptionIndex;

			_inputTypeOptionButtons[i].Outline.color = active ? Color.white : _inactiveButtonColor;
			_inputTypeOptionButtons[i].Check.enabled = active;
		}
	}

	public void ForceLayoutRebuilding(RectTransform transformToRebuild)
	{
		var verticalLayoutGroup = transformToRebuild.GetComponent<VerticalLayoutGroup>();
		var horizontalLayoutGroup = transformToRebuild.GetComponent<HorizontalLayoutGroup>();

		LayoutRebuilder.ForceRebuildLayoutImmediate(transformToRebuild);
		LayoutRebuilder.MarkLayoutForRebuild(transformToRebuild);

		if (verticalLayoutGroup != null)
		{
			verticalLayoutGroup.CalculateLayoutInputVertical();
			verticalLayoutGroup.SetLayoutVertical();
		}

		if (horizontalLayoutGroup != null)
		{
			horizontalLayoutGroup.CalculateLayoutInputHorizontal();
			horizontalLayoutGroup.SetLayoutHorizontal();
		}
	}

	public void UI_SaveInputTypeSelection(int inputTypeOptionIndex)
	{
		InputTypeSelection = inputTypeOptionIndex;

		SelectInputTypeOption(inputTypeOptionIndex - 1);
	}

	public void UI_SaveColorPalette(int colorPaletteIndex)
	{
		ColorPaletteSelection = colorPaletteIndex;

		SelectColorPalette(colorPaletteIndex - 1);
	}

	public void UI_OpenConfirmClearProgress()
	{
		ToggleSubPanel(_confirmClearProgressPanel, true, true);
	}

	public void UI_ConfirmClearProgress()
	{
		MenuPanel.Instance.ProgressionIndex = 0;

		GamePanel.Instance.CurrentProgressionGrid = null;
		
		ToggleSubPanel(_clearProgressionButton, false, false);
		ToggleSubPanel(_confirmClearProgressPanel, false, true);
	}

	public void UI_CancelClearProgress()
	{
		ToggleSubPanel(_confirmClearProgressPanel, false, true);
	}

	public void UI_Close()
	{
		MenuPanel.Instance.Show(_fromCustom, false);
	}

	private void ToggleSubPanel(CanvasGroup panel, bool toggle, bool animate)
	{
		panel.interactable = toggle;
		panel.blocksRaycasts = toggle;

		if (animate)
		{
			panel.DOFade(toggle ? 1f : 0f, _panelTransitionTime);
		}
		else
		{
			panel.alpha = toggle ? 1f : 0f;
		}
	}
}
