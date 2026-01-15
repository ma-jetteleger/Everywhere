using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;

// TODO : Make sure that there is an almost equal number of node of each color
// - Check how that affects the shape count/progression !

// TODO : Publishing
// - ...


public enum InputType
{
	DragAndRelease = 1,
	TapAndHold = 2
}

public class Player : MonoBehaviour
{
	[SerializeField] private TriangleGrid _grid = null;
	[SerializeField] private Transform _cursor = null;
	[SerializeField] private float _nodeHoldBuffer = 0f;
	[SerializeField] private float _nodeHoldTime = 0f;
	
	public bool Busy => _currentShape != null;

	public InputType InputType
	{
		get
		{
			if(_inputType == 0)
			{
				_inputType = (InputType)SettingsPanel.Instance.InputTypeSelection;
			}

			return _inputType;
		}
		set
		{
			_inputType = value;
		}
	}

	private List<Node> _currentShape;
	private LineRenderer _shapeLineRenderer;
	private ShapeGenerator _shapeGenerator;
	private bool _shapeComplete;
	private float _nodeHoldTimer;
	private Node _highlightedNode;
	private List<Node> _pressedNodesSinceLastLift;
	private bool _clickedOnUi;
	private bool _needsToLift;
	private InputType _inputType;

	private void Start()
	{
		_pressedNodesSinceLastLift = new List<Node>();
	}

	private void Awake()
	{
		_shapeLineRenderer = GetComponentInChildren<LineRenderer>();
	}
	
	private void Update()
    {
		var mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
		var mousePos2D = new Vector2(mousePos.x, mousePos.y);

		if (Input.GetMouseButtonDown(0))
		{
			_clickedOnUi = IsPointerOverUIObject();

			if(!_clickedOnUi && InputType == InputType.TapAndHold && !_shapeComplete && _currentShape != null)
			{
				_shapeLineRenderer.positionCount++;
				_shapeLineRenderer.SetPosition(_shapeLineRenderer.positionCount - 1, _shapeLineRenderer.GetPosition(_shapeLineRenderer.positionCount - 2));
			}
		}
		
		if (!_shapeComplete && Input.GetMouseButton(0) && !_clickedOnUi)
		{
			var nodeDragged = Physics2D.Raycast(mousePos2D, Vector2.zero, Mathf.Infinity, LayerMask.GetMask("Node")).collider?.gameObject?.GetComponentInParent<Node>();

			if (nodeDragged != null && !nodeDragged.Grey)
			{
				switch (InputType)
				{
					case InputType.DragAndRelease:

						if (_currentShape == null)
						{
							_currentShape = new List<Node>();
							_currentShape.Add(nodeDragged);

							_shapeLineRenderer.positionCount = 2;
							_shapeLineRenderer.SetPosition(0, nodeDragged.transform.position);

							nodeDragged.Punch(true);
						}
						else if ((!_currentShape.Contains(nodeDragged) || (nodeDragged == _currentShape[0] && _currentShape.Count > 2)) && nodeDragged.Color == _currentShape[0].Color)
						{
							_currentShape.Add(nodeDragged);
							
							_shapeLineRenderer.SetPosition(_shapeLineRenderer.positionCount - 1, nodeDragged.transform.position);

							if (nodeDragged != _currentShape[0])
							{
								_shapeLineRenderer.positionCount++;
							}
							else
							{
								_shapeComplete = true;
							}

							nodeDragged.Punch(false);
						}
						
						break;

					case InputType.TapAndHold:

						if (!_needsToLift
							&& nodeDragged != _highlightedNode 
							&& (!_pressedNodesSinceLastLift.Contains(nodeDragged) || (_currentShape != null && _currentShape.Count > 2 && nodeDragged == _currentShape[0]))
							//&& (_currentShape == null || _currentShape.Count > 2 || (_currentShape.Count <= 2 && nodeDragged != _currentShape[0]))
							&& !_clickedOnUi)
						{
							CancelHighlight();

							_highlightedNode = nodeDragged;
							_highlightedNode.State = (_currentShape != null && _currentShape[0] == _highlightedNode) ? NodeState.HoveredShapeStart : NodeState.Hovered;

							if (!_shapeComplete && _currentShape != null)
							{
								_shapeLineRenderer.SetPosition(_shapeLineRenderer.positionCount - 1, _highlightedNode.transform.position);
							}

							_nodeHoldTimer = 0f;
						}
						
						break;
				}
			}
			else
			{
				CancelHighlight();
			}

			if (InputType == InputType.TapAndHold 
				&& !_shapeComplete 
				&& _currentShape != null 
				&& _highlightedNode == null 
				&& !_clickedOnUi)
			{
				_shapeLineRenderer.SetPosition(_shapeLineRenderer.positionCount - 1, mousePos2D);
			}

			if (InputType == InputType.DragAndRelease
				&& !_shapeComplete
				&& _currentShape != null
				//&& _highlightedNode == null
				&& !_clickedOnUi)
			{
				_shapeLineRenderer.SetPosition(_shapeLineRenderer.positionCount - 1, mousePos2D);
			}
		}

		if (Input.GetMouseButtonUp(0))
		{
			if (_clickedOnUi)
			{
				_clickedOnUi = false;

				return;
			}

			switch (InputType)
			{
				case InputType.DragAndRelease:

					CheckForCompleteShape();

					break;

				case InputType.TapAndHold:
					
					if (_highlightedNode != null)
					{
						ClickOnNode();
					}
					else if(!_shapeComplete && _currentShape != null)
					{
						_shapeLineRenderer.positionCount--;
					}

					_needsToLift = false;

					_pressedNodesSinceLastLift.Clear();

					_cursor.gameObject.SetActive(false);

					break;
			}
			
			CancelHighlight();
		}

		if (_highlightedNode != null 
			&& (_currentShape == null || (_currentShape[0] == _highlightedNode/* && _currentShape.Count > 2*/)
				|| (!_currentShape.Contains(_highlightedNode) 
					&& (_currentShape[0].Color == _highlightedNode.Color)))
			&& _nodeHoldTimer < _nodeHoldBuffer + _nodeHoldTime 
			&& !_clickedOnUi)
		{
			_nodeHoldTimer += Time.deltaTime;

			if(_nodeHoldTimer >= _nodeHoldBuffer)
			{
				_highlightedNode.UpdateHeldFeedback((_nodeHoldTimer - _nodeHoldBuffer) / _nodeHoldTime);

				if (_nodeHoldTimer >= _nodeHoldBuffer + _nodeHoldTime)
				{
					ClickOnNode();

					var pressedNode = _highlightedNode;

					_shapeLineRenderer.positionCount++;
					_shapeLineRenderer.SetPosition(_shapeLineRenderer.positionCount - 1, mousePos2D);
					
					_pressedNodesSinceLastLift.Add(pressedNode);

					_highlightedNode.State = (_currentShape != null && _currentShape[0] == _highlightedNode) ? NodeState.ShapeStart : NodeState.Normal;
					_highlightedNode.ResetHeldFeedback();

					_highlightedNode = null;
					_nodeHoldTimer = 0f;
				}
			}
		}

		if (Input.GetMouseButton(0) && (_currentShape != null && _currentShape.Count > 0 && !_shapeComplete) && !_cursor.gameObject.activeSelf)
		{
			_cursor.gameObject.SetActive(true);
		}
		else if ((_currentShape == null || _currentShape.Count == 0 || _shapeComplete) && _cursor.gameObject.activeSelf)
		{
			_cursor.gameObject.SetActive(false);
		}
		
		if (_cursor.gameObject.activeSelf)
		{
			_cursor.position = mousePos2D;
		}
		
		/*if (!_shapeComplete && _currentShape != null && _highlightedNode == null && !_clickedOnUi)
		{
			_shapeLineRenderer.SetPosition(_shapeLineRenderer.positionCount - 1, mousePos2D);
		}*/
	}
	
	private void ClickOnNode()
	{
		if (_currentShape == null)
		{
			_currentShape = new List<Node>
			{
				_highlightedNode
			};

			_shapeLineRenderer.positionCount = 1;
			_shapeLineRenderer.SetPosition(0, _highlightedNode.transform.position);
			
			_highlightedNode.State = NodeState.ShapeStart;
		}
		else if (_highlightedNode == _currentShape[0] && _currentShape.Count <= 2)
		{
			_highlightedNode.State = NodeState.Normal;

			CheckForCompleteShape();
		}
		else if (_currentShape.Contains(_highlightedNode) && _highlightedNode != _currentShape[0])
		{
			_currentShape.Remove(_highlightedNode);

			_highlightedNode.State = NodeState.Normal;

			_shapeLineRenderer.positionCount -= 2;
			_shapeLineRenderer.SetPositions(_currentShape.Select(x => x.transform.position).ToArray());
		}
		else if (_highlightedNode.Color == _currentShape[0].Color)
		{
			_currentShape.Add(_highlightedNode);

			_shapeLineRenderer.SetPosition(_shapeLineRenderer.positionCount - 1, _highlightedNode.transform.position);
			
			if (_highlightedNode == _currentShape[0])
			{
				_highlightedNode.State = NodeState.Normal;

				_shapeComplete = true;

				CheckForCompleteShape();

				_needsToLift = true;
			}
		}
		else
		{
			_shapeLineRenderer.positionCount--;
		}

		GamePanel.Instance.UpdateCancelShapeButton();
	}

	private void CheckForCompleteShape()
	{
		if (_shapeComplete)
		{
			var simplifiedShape = _grid.SimplifyShape(_currentShape);
			var sameShape = CheckForSameShape(simplifiedShape, _currentShape[0].Color);
			var isValid = !sameShape && _grid.ValidateShape(simplifiedShape, simplifiedShape.Length);

			if (isValid)
			{
				Camera.main.transform.position = new Vector3(Camera.main.transform.position.x, Camera.main.transform.position.y, Camera.main.transform.position.z - 0.01f);

				_grid.AddShape(simplifiedShape, _currentShape[0].Color, (ShapeKind)simplifiedShape.Length, false);
			}
		}

		Cleanup();
	}

	private void CancelHighlight()
	{
		if(_highlightedNode != null)
		{
			_highlightedNode.State = (_currentShape != null && _currentShape[0] == _highlightedNode) ? NodeState.ShapeStart : NodeState.Normal;
			_highlightedNode.ResetHeldFeedback();

			_highlightedNode = null;
		}
		
		_nodeHoldTimer = 0f;
	}

	public void Cleanup()
	{
		if (_currentShape != null)
		{
			foreach(var node in _currentShape)
			{
				node.State = NodeState.Normal;
			}

			_currentShape.Clear();
			_currentShape = null;
		}

		_shapeLineRenderer.positionCount = 0;
		_clickedOnUi = false;
		_shapeComplete = false;
	}

	private bool IsPointerOverUIObject()
	{
		var eventDataCurrentPosition = new PointerEventData(EventSystem.current)
		{
			position = new Vector2(Input.mousePosition.x, Input.mousePosition.y)
		};

		var results = new List<RaycastResult>();

		EventSystem.current.RaycastAll(eventDataCurrentPosition, results);

		return results.Count > 0;
	}

	private bool CheckForSameShape(Vector3[] newVertices, NodeColor color)
	{
		foreach (var oldShape in _grid.Shapes)
		{
			if (color != oldShape.Color || newVertices.Length != oldShape.Vertices.Count)
			{
				continue;
			}

			var samePoints = 0;

			foreach (var newVertex in newVertices)
			{
				var newPoint = new Vector2(newVertex.x, newVertex.y);
				
				foreach (var oldPoint in oldShape.Points)
				{
					if(Vector2.Distance(newPoint, oldPoint) <= (_grid.NodeDistance / 2f))
					{
						samePoints++;

						break;
					}
				}
			}

			if (samePoints == oldShape.Points.Count)
			{
				_grid.BringToFront(oldShape);

				return true;
			}
		}

		return false;
	}
}
