using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;
using DG.Tweening;

public class MenuPanel : MonoBehaviour
{
	public static MenuPanel Instance { get; set; }

	public const string kProgressionIndex = "ProgressionIndex";
	public const string kCustomWidth = "CustomWidth";
	public const string kCustomHeight = "CustomHeight";
	public const string kCustomColors = "CustomColors";
	public const string kCustomDifficulty = "CustomDifficulty";

	[SerializeField] private TextMeshProUGUI _playProgressionLabel = null;
	[SerializeField] private CanvasGroup _mainSection = null;
	[SerializeField] private CanvasGroup _customSection = null;
	[SerializeField] private CanvasGroup _customBackButton = null;
	[SerializeField] private CanvasGroup _confirmClearCustomPanel = null;
	[SerializeField] private float _confirmPanelTransitionTime = 0f;
	[SerializeField] private Button _playCustomButton = null;
	[SerializeField] private GameObject _otherCustomButton = null;
	[SerializeField] private CustomParameterChooser _widthChooser = null;
	[SerializeField] private CustomParameterChooser _heightChooser = null;
	[SerializeField] private CustomParameterChooser _colorChooser = null;
	[SerializeField] private CustomParameterChooser _difficultyChooser = null;
	[SerializeField] private ColorBlock[] _colorBlocks = null;
	[SerializeField] private int _defaultWidth = 0;
	[SerializeField] private int _defaultHeight = 0;
	[SerializeField] private int _defaultColors = 0;
	[SerializeField] private int _defaultDifficulty = 0;

	public int ProgressionIndex
	{
		get
		{
			if (_progressionIndex == 0)
			{
				_progressionIndex = PlayerPrefs.GetInt(kProgressionIndex);
			}

			return _progressionIndex;
		}
		set
		{
			_progressionIndex = value;
			PlayerPrefs.SetInt(kProgressionIndex, _progressionIndex);
		}
	}

	public int CustomWidth
	{
		get
		{
			if (_customWidth == 0)
			{
				_customWidth = PlayerPrefs.GetInt(kCustomWidth);
			}

			return _customWidth;
		}
		set
		{
			_customWidth = value;
			PlayerPrefs.SetInt(kCustomWidth, _customWidth);
		}
	}

	public int CustomHeight
	{
		get
		{
			if (_customHeight == 0)
			{
				_customHeight = PlayerPrefs.GetInt(kCustomHeight);
			}

			return _customHeight;
		}
		set
		{
			_customHeight = value;
			PlayerPrefs.SetInt(kCustomHeight, _customHeight);
		}
	}

	public int CustomColors
	{
		get
		{
			if (_customColors == 0)
			{
				_customColors = PlayerPrefs.GetInt(kCustomColors);
			}

			return _customColors;
		}
		set
		{
			_customColors = value;
			PlayerPrefs.SetInt(kCustomColors, _customColors);
		}
	}

	public int CustomDifficulty
	{
		get
		{
			if (_customDifficulty == 0)
			{
				_customDifficulty = PlayerPrefs.GetInt(kCustomDifficulty);
			}

			return _customDifficulty;
		}
		set
		{
			_customDifficulty = value;
			PlayerPrefs.SetInt(kCustomDifficulty, _customDifficulty);
		}
	}

	public CanvasGroup Panel { get; set; }

	private int _progressionIndex;
	private int _customWidth;
	private int _customHeight;
	private int _customColors;
	private int _customDifficulty;
	private CustomParameterChooser[] _choosers;
	private bool _dirtyCustomConfiguration;
	
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
		var widthValue = CustomWidth == 0 ? _defaultWidth : CustomWidth;
		var heightValue = CustomHeight == 0 ? _defaultHeight : CustomHeight;
		var colorsValue = CustomColors == 0 ? _defaultColors : CustomColors;
		var difficultyValue = CustomDifficulty == 0 ? _defaultDifficulty : CustomDifficulty;

		_choosers = new CustomParameterChooser[] { _widthChooser, _heightChooser, _colorChooser, _difficultyChooser };
		var values = new int[] { widthValue, heightValue, colorsValue, difficultyValue };

		for (var i = 0; i < _choosers.Length; i++)
		{
			_choosers[i].Value = values[i];
		}
		
		Show(false, true);
	}

	private void Update()
	{
		// TODO : [TEMP] Take this out before release

		/*if(Input.GetKeyDown(KeyCode.UpArrow))
		{
			ProgressionIndex++;

			GamePanel.Instance.CurrentProgressionGrid = null;

			_playProgressionLabel.text = $"Play \nLevel {ProgressionIndex + 1}";
		}
		if (Input.GetKeyDown(KeyCode.DownArrow) && ProgressionIndex > 0)
		{
			ProgressionIndex--;

			GamePanel.Instance.CurrentProgressionGrid = null;

			_playProgressionLabel.text = $"Play \nLevel {ProgressionIndex + 1}";
		}*/

		// TODO : [TEMP] Take this out before release
	}

	public void Show(bool custom, bool gameStart)
	{
		ToggleSubPanel(_mainSection, !custom, !gameStart);
		ToggleSubPanel(_customSection, custom, !gameStart);
		ToggleSubPanel(_customBackButton, custom, !gameStart);
		
		ToggleSubPanel(Panel, true, !gameStart);
		ToggleSubPanel(GamePanel.Instance.Panel, false, !gameStart);
		ToggleSubPanel(SettingsPanel.Instance.Panel, false, !gameStart);
		
		foreach (var colorBlock in _colorBlocks)
		{
			colorBlock.Shuffle((Color[])SettingsPanel.Instance.SelectedColorPalette.Clone());
		}
		
		UpdateProgressionElements();

		ToggleSubPanel(_confirmClearCustomPanel, false, false);
	}

	private void UpdateProgressionElements()
	{
		var currentProgressionGrid = GamePanel.Instance.CurrentProgressionGrid;
		
		var playedProgression = ProgressionIndex > 0;
		var isContinuing = !string.IsNullOrEmpty(currentProgressionGrid) && !string.IsNullOrEmpty(currentProgressionGrid.Split(';')[6]);
		
		_playProgressionLabel.text = playedProgression
			? isContinuing
				? $"Continue \nLevel {GamePanel.Instance.CurrentProgressionGrid.Split(';')[0]}"
				: $"Play \nLevel {GamePanel.Instance.CurrentProgressionGrid.Split(';')[0]}"
			: "Play \nLevel 1";

		var customCustomGrid = GamePanel.Instance.CurrentCustomGrid;
		var playedCustom = !string.IsNullOrEmpty(customCustomGrid) && !string.IsNullOrEmpty(customCustomGrid.Split(';')[6]);

		_otherCustomButton.SetActive(playedCustom);
		_playCustomButton.gameObject.SetActive(!playedCustom);

		foreach (var chooser in _choosers)
		{
			chooser.Interactable = !playedCustom;
		}
	}

	public void UI_Play()
	{
		GamePanel.Instance.Show(ProgressionIndex);
	}
	
	public void UI_PlayCustom()
	{
		if(_dirtyCustomConfiguration)
		{
			GamePanel.Instance.CurrentCustomGrid = null;

			_dirtyCustomConfiguration = false;
		}

		GamePanel.Instance.Show(CustomWidth, CustomHeight, CustomColors, CustomDifficulty);
	}
	
	public void UI_SaveCustomWidth()
	{
		SaveCustomWidth();
	}

	public void UI_SaveCustomHeight()
	{
		SaveCustomHeight();
	}

	public void UI_SaveCustomColors()
	{
		SaveCustomColors();
	}

	public void UI_SaveCustomDifficulty()
	{
		SaveCustomDifficulty();
	}

	public void UI_OpenSettings()
	{
		SettingsPanel.Instance.Show(_customSection.interactable);
	}

	public void UI_OpenCustom()
	{
		ToggleSubPanel(_customSection, true, true);
		ToggleSubPanel(_customBackButton, true, true);
		ToggleSubPanel(_mainSection, false, true);
	}

	public void UI_CloseCustom()
	{
		ToggleSubPanel(_customSection, false, true);
		ToggleSubPanel(_customBackButton, false, true);
		ToggleSubPanel(_mainSection, true, true);
	}
	
	public void UI_OpenConfirmClearCustom()
	{
		ToggleSubPanel(_confirmClearCustomPanel, true, true);
	}

	public void UI_ConfirmClearCustom()
	{
		GamePanel.Instance.CurrentCustomGrid = null;

		UpdateProgressionElements();

		ToggleSubPanel(_confirmClearCustomPanel, false, true);
	}

	public void UI_CancelClearCustom()
	{
		ToggleSubPanel(_confirmClearCustomPanel, false, true);
	}

	private void SaveCustomWidth()
	{
		var newValue = _widthChooser.Value;

		if(CustomWidth != newValue)
		{
			CustomWidth = newValue;

			_dirtyCustomConfiguration = true;
		}
	}

	private void SaveCustomHeight()
	{
		var newValue = _heightChooser.Value;

		if (CustomHeight != newValue)
		{
			CustomHeight = newValue;

			_dirtyCustomConfiguration = true;
		}
	}

	private void SaveCustomColors()
	{
		var newValue = _colorChooser.Value;

		if (CustomColors != newValue)
		{
			CustomColors = newValue;

			_dirtyCustomConfiguration = true;
		}
	}

	public void SaveCustomDifficulty()
	{
		var newValue = _difficultyChooser.Value;

		if (CustomDifficulty != newValue)
		{
			CustomDifficulty = newValue;

			_dirtyCustomConfiguration = true;
		}
	}

	private void ToggleSubPanel(CanvasGroup panel, bool toggle, bool animate)
	{
		panel.interactable = toggle;
		panel.blocksRaycasts = toggle;

		if (animate)
		{
			panel.DOFade(toggle ? 1f : 0f, _confirmPanelTransitionTime);
		}
		else
		{
			panel.alpha = toggle ? 1f : 0f;
		}
	}
}
