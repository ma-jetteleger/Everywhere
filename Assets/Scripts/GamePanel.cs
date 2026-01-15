using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using System;

public class GamePanel : MonoBehaviour
{
	public static GamePanel Instance { get; set; }

	public const string kCurrentProgressionGrid = "CurrentProgressionGrid";
	public const string kCurrentCustomGrid = "CurrentCustomGrid";
	public const string kSeenSettingsMessage = "SeenSettingsMessage";
	
	[SerializeField] private GameObject _objectiveTrackerTemplate = null;
	[SerializeField] private Transform _objectiveLoadingImage = null;
	[SerializeField] private TextMeshProUGUI _gridLevelText = null;
	[SerializeField] private CanvasGroup _nextButton = null;
	[SerializeField] private CanvasGroup _nextCustomButton = null;
	[SerializeField] private CanvasGroup _resetButton = null;
	[SerializeField] private CanvasGroup _cancelShapeButton = null;
	[SerializeField] private CanvasGroup _settingsMessagePanel = null;
	[SerializeField] private CanvasGroup _confirmResetGridPanel = null;
	[SerializeField] private float _confirmPanelTransitionTime = 0f;
	[SerializeField] private ColorBlock[] _colorBlocks = null;
	[SerializeField] private CompletedShapeFeedback[] _completedShapeFeedbackOptions = null;
	[SerializeField] private RectTransform _shapeFeedbackTopAnchor = null;
	[SerializeField] private RectTransform _shapeFeedbackBottomAnchor = null;
	[SerializeField] private GameObject _gridFeedbackTemplate = null;
	[SerializeField] private int _gridFeedbackAmount = 0;
	[SerializeField] private TriangleGrid _grid = null;
	[SerializeField] private Player _player = null;

	public CanvasGroup NextButton => _nextButton;
	public CanvasGroup NextCustomButton => _nextCustomButton;
	public CanvasGroup ResetButton => _resetButton;

	public string CurrentProgressionGrid
	{
		get
		{
			if (_currentProgressionGrid == null)
			{
				_currentProgressionGrid = PlayerPrefs.GetString(kCurrentProgressionGrid);
			}

			return _currentProgressionGrid;
		}
		set
		{
			_currentProgressionGrid = value;
			PlayerPrefs.SetString(kCurrentProgressionGrid, _currentProgressionGrid);
		}
	}

	public string CurrentCustomGrid
	{
		get
		{
			if (_currentCustomGrid == null)
			{
				_currentCustomGrid = PlayerPrefs.GetString(kCurrentCustomGrid);
			}

			return _currentCustomGrid;
		}
		set
		{
			_currentCustomGrid = value;
			PlayerPrefs.SetString(kCurrentCustomGrid, _currentCustomGrid);
		}
	}

	public int SeenSettingsMessage
	{
		get
		{
			if (_seenSettingsMessage == null)
			{
				_seenSettingsMessage = PlayerPrefs.GetInt(kSeenSettingsMessage);
			}

			return _seenSettingsMessage.Value;
		}
		set
		{
			_seenSettingsMessage = value;
			PlayerPrefs.SetInt(kSeenSettingsMessage, _seenSettingsMessage.Value);
		}
	}

	public Dictionary<ShapeKind, ObjectiveTracker> ObjectiveTrackers { get; set; }
	public CanvasGroup Panel { get; set; }

	private string _currentProgressionGrid;
	private string _currentCustomGrid;
	private int? _seenSettingsMessage;
	private List<GridFeedbackShape> _gridFeedbackShapes;
	
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
		ToggleSubPanel(Panel, false, false);

		_gridFeedbackShapes = new List<GridFeedbackShape>
		{
			_gridFeedbackTemplate.GetComponent<GridFeedbackShape>()
		};

		for (var i = 0; i < _gridFeedbackAmount; i++)
		{
			_gridFeedbackShapes.Add(Instantiate(_gridFeedbackTemplate, _gridFeedbackTemplate.transform.parent).GetComponent<GridFeedbackShape>());
		}
	}

	private void Update()
	{
		if(_objectiveLoadingImage.gameObject.activeSelf)
		{
			_objectiveLoadingImage.Rotate(Vector3.back, Time.deltaTime * 100f);
		}

		// TODO : [TEMP] Take this out before release

		/*if (Input.GetKeyDown(KeyCode.Space))
		{
			ShowCompletedGridFeedback();
		}*/

		// TODO : [TEMP] Take this out before release
	}

	public void Initialize(int? level)
	{
		_objectiveTrackerTemplate.SetActive(false);

		ToggleSubPanel(NextButton, false, true);
		ToggleSubPanel(NextCustomButton, false, true);
		ToggleSubPanel(ResetButton, false, true);
		ToggleSubPanel(_cancelShapeButton, false, true);
		
		_objectiveLoadingImage.gameObject.SetActive(true);

		if(level == null)
		{
			_gridLevelText.transform.parent.gameObject.SetActive(false);
			_settingsMessagePanel.gameObject.SetActive(false);
		}
		else
		{
			_gridLevelText.text = $"Level {level.Value}";
			_gridLevelText.transform.parent.gameObject.SetActive(true);

			if(level == 2 && SeenSettingsMessage == 0)
			{
				_settingsMessagePanel.gameObject.SetActive(true);

				OpenSettingsMessage();
			}
			else
			{
				_settingsMessagePanel.gameObject.SetActive(false);
			}
		}
		
		foreach (var feedback in _completedShapeFeedbackOptions)
		{
			feedback.transform.position = _shapeFeedbackBottomAnchor.position;
		}
	}

	public void Show(int progressionIndex)
	{
		Show();
		
		if (!string.IsNullOrEmpty(CurrentProgressionGrid))
		{
			_grid.Initialize(CurrentProgressionGrid, false);
		}
		else
		{
			_grid.Initialize(progressionIndex);
		}
	}

	public void Show(int width, int height, int colors, int difficulty)
	{
		Show();

		if (!string.IsNullOrEmpty(CurrentCustomGrid))
		{
			_grid.Initialize(CurrentCustomGrid, true);
		}
		else
		{
			_grid.Initialize(width, height, colors, difficulty * 10, true);
		}
	}

	public void Show()
	{
		CleanUp();

		ToggleSubPanel(Panel, true, true);
		ToggleSubPanel(MenuPanel.Instance.Panel, false, true);
		
		foreach (var colorBlock in _colorBlocks)
		{
			colorBlock.Shuffle((Color[])SettingsPanel.Instance.SelectedColorPalette.Clone());
		}

		ToggleSubPanel(_confirmResetGridPanel, false, false);
	}
	
	public void InitializeObjectives()
	{
		CleanUp();
		
		ObjectiveTrackers = new Dictionary<ShapeKind, ObjectiveTracker>();

		if (_grid.Objectives != null)
		{
			foreach (var objective in _grid.Objectives.Values)
			{
				if(objective == null || objective.Target == 0)
				{
					continue;
				}

				var newObjectiveTracker = Instantiate(_objectiveTrackerTemplate, _objectiveTrackerTemplate.transform.parent).GetComponent<ObjectiveTracker>();
				newObjectiveTracker.Initialize(objective);

				ObjectiveTrackers.Add(objective.ShapeKind, newObjectiveTracker);

				newObjectiveTracker.gameObject.SetActive(true);
			}
		}

		_objectiveLoadingImage.gameObject.SetActive(false);
		
		ToggleSubPanel(ResetButton, !NextButton.interactable && !NextCustomButton.interactable, true);
		
		ForceLayoutRebuilding(_objectiveTrackerTemplate.transform.parent.GetComponent<RectTransform>());
	}
	
	public void UpdateObjectiveTracker(ShapeKind shapeKind/*, NodeColor color*/)
	{
		if(ObjectiveTrackers == null || !ObjectiveTrackers.ContainsKey(shapeKind) || ObjectiveTrackers[shapeKind] == null)
		{
			return;
		}

		ObjectiveTrackers[shapeKind].UpdateCounter();

		ForceLayoutRebuilding(ObjectiveTrackers[shapeKind].RectTransform);
	}

	public void ShowCompletedShapeFeedback(Vector3 shapeWorldPosition, ShapeKind shapeKind, NodeColor color)
	{
		var shapePosition = Camera.main.WorldToScreenPoint(shapeWorldPosition);
		var topAnchorPosition = _shapeFeedbackTopAnchor.position;
		var bottomAnchorPosition = _shapeFeedbackBottomAnchor.position;
		topAnchorPosition.x = shapePosition.x;
		bottomAnchorPosition.x = shapePosition.x;

		_shapeFeedbackTopAnchor.position = topAnchorPosition;
		_shapeFeedbackBottomAnchor.position = bottomAnchorPosition;

		foreach (var feedback in _completedShapeFeedbackOptions)
		{
			if(feedback.Busy)
			{
				continue;
			}

			feedback.Show(_shapeFeedbackTopAnchor.position, _shapeFeedbackBottomAnchor.position, shapePosition, shapeKind, _grid.NodeColorMap[color]);

			break;
		}
		
		if (ObjectiveTrackers != null && ObjectiveTrackers.ContainsKey(shapeKind))
		{
			ObjectiveTrackers[shapeKind].ShowCompletedShapeFeedback(_grid.NodeColorMap[color]);
		}
	}

	public void UpdateCancelShapeButton()
	{
		ToggleSubPanel(_cancelShapeButton, _player.InputType == InputType.TapAndHold && _player.Busy, true);
	}

	private void CleanUp()
	{
		if(ObjectiveTrackers != null)
		{
			foreach (var tracker in ObjectiveTrackers)
			{
				DestroyImmediate(tracker.Value.gameObject);
			}

			ObjectiveTrackers.Clear();
		}

		foreach (var gridFeedbackShape in _gridFeedbackShapes)
		{
			gridFeedbackShape.Hide();
		}

		foreach (var completedShapeFeedback in _completedShapeFeedbackOptions)
		{
			completedShapeFeedback.Hide();
		}
	}

	public void InitializeGridFeedback(ShapeKind[] shapeKinds, Color[] colors)
	{
		foreach (var gridFeedbackShape in _gridFeedbackShapes)
		{
			gridFeedbackShape.Initialize(shapeKinds, colors, _shapeFeedbackBottomAnchor.position.y, _shapeFeedbackTopAnchor.position.y);
		}
	}

	public void ShowCompletedGridFeedback()
	{
		foreach(var gridFeedbackShape in _gridFeedbackShapes)
		{
			gridFeedbackShape.Show();
		}
	}

	public void ForceLayoutRebuilding(RectTransform transformToRebuild)
	{
		var verticalLayoutGroup = transformToRebuild.GetComponent<VerticalLayoutGroup>();
		var horizontalLayoutGroup = transformToRebuild.GetComponent<HorizontalLayoutGroup>();

		LayoutRebuilder.ForceRebuildLayoutImmediate(transformToRebuild);
		LayoutRebuilder.MarkLayoutForRebuild(transformToRebuild);

		if(verticalLayoutGroup != null)
		{
			verticalLayoutGroup.CalculateLayoutInputVertical();
			verticalLayoutGroup.SetLayoutVertical();
		}
		
		if(horizontalLayoutGroup != null)
		{
			horizontalLayoutGroup.CalculateLayoutInputHorizontal();
			horizontalLayoutGroup.SetLayoutHorizontal();
		}
	}

	public void UI_BackToMenu()
	{
		CleanUp();

		_player.Cleanup();
		_grid.CleanUp(false, false);

		if(_settingsMessagePanel.gameObject.activeSelf && _settingsMessagePanel.alpha > 0.1f)
		{
			ToggleSubPanel(_settingsMessagePanel, false, false);
		}
		
		MenuPanel.Instance.Show(_grid.CustomGrid, false);
	}

	public void OpenSettingsMessage()
	{
		SeenSettingsMessage = 1;

		ToggleSubPanel(_settingsMessagePanel, true, true);
	}

	public void UI_CloseSettingsMessage()
	{
		ToggleSubPanel(_settingsMessagePanel, false, true);
	}

	public void UI_OpenResetGrid()
	{
		if(_grid.Shapes.Count > 0)
		{
			ToggleSubPanel(_confirmResetGridPanel, true, true);
		}
		else
		{
			UI_ConfirmResetGrid();
		}
	}

	public void UI_ConfirmResetGrid()
	{
		CleanUp();

		_player.Cleanup();
		_grid.CleanUp(true, false);
		
		if(!_grid.CustomGrid && MenuPanel.Instance.ProgressionIndex == 1)
		{
			_grid.Initialize(MenuPanel.Instance.ProgressionIndex);
		}
		else
		{
			try
			{
#if UNITY_WEBGL
			StartCoroutine(_grid.Initialize());
#else
				_ = _grid.Initialize();
#endif
			}
			catch (Exception e)
			{
				Debug.LogWarning(e);

				throw;
			}
		}
		
		ToggleSubPanel(_confirmResetGridPanel, false, true);
	}

	public void UI_CancelResetGrid()
	{
		ToggleSubPanel(_confirmResetGridPanel, false, true);
	}

	public void UI_NextGrid()
	{
		CleanUp();

		_player.Cleanup();
		_grid.CleanUp(false, false);
		
		_grid.Initialize(MenuPanel.Instance.ProgressionIndex);
	}

	public void UI_CancelShape()
	{
		_player.Cleanup();

		UpdateCancelShapeButton();
	}

	public void ToggleSubPanel(CanvasGroup subPanel, bool toggle, bool animate)
	{
		subPanel.interactable = toggle;
		subPanel.blocksRaycasts = toggle;

		if(animate)
		{
			subPanel.DOFade(toggle ? 1f : 0f, _confirmPanelTransitionTime);
		}
		else
		{
			subPanel.alpha = toggle ? 1f : 0f;
		}
	}
}
