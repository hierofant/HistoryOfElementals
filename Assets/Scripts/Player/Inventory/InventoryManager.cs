using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using System;
using TMPro;
using Player;
using Game.Items;
using Debug = UnityEngine.Debug;
using Unity.VisualScripting;

namespace Inventory
{
    public class InventoryManager : MonoBehaviour
    {
        public class InventoryItemCell
        {
            public ItemStack ItemStack = null; 
            public RectTransform ItemContainer = null;
            public TMP_Text ItemCountText = null;
            public Image ItemSprite = null;
        }
        [SerializeField] private RectTransform[] _cellsUI; // ������ ��������� (������� � �������� ������)
        [SerializeField] private GameObject _cellHighlight; // ��������� ������� ������
        [SerializeField] private GameObject _inventoryUI; // ������ ���������
        [SerializeField] private GameObject _countUIText; // ����� ����������
        [SerializeField] private GameObject _imageUI; // ����� ����������
        [SerializeField] private GameObject _thrownObject; // ������ ������������ ��������
        [SerializeField] private int _currentCell = 0; // ������� ��������� ������ � ������� ������
        [SerializeField] private int _hotbarSize = 5; // ���������� ������ � ������� ����
        private InventoryItemCell[] _cells; // ��� ����� ��������� (� ������� ���, � �������� ���������)
        private InputManager _inputManager; // ���������� ������
        private bool _isInventoryOpen = false; // ��������� ���������
        private bool _isDraggingItem = false; // ������ �������������� ��������
        private GameObject _draggedItemObject; // ������ ���������������� ��������
        private ItemStack _draggedItem;
        private PlayerController2D _playerController; // ��� ���������� �������� ��� �������� ���������
        private bool _isFirstClick;

        public static event Action<ItemStack, int> OnItemUpdated; // ������� ��� ���������� �������� � ������

        private void Awake()
        {
            _cells = new InventoryItemCell[_cellsUI.Length];
            for (int i = 0; i < _cells.Length; i++)
            {
                _cells[i] = new InventoryItemCell();
            }
            for (int i = 0; i < _cells.Length; i++)
            {
                _cells[i].ItemContainer = _cellsUI[i];
                _cells[i].ItemCountText = _cellsUI[i].GetComponentInChildren<TMP_Text>();
            }

            _cellHighlight.GetComponent<RectTransform>().position = _cellsUI[_currentCell].GetComponent<RectTransform>().position;

            _inputManager = new InputManager();
            _inputManager.UI.Enable();
            _playerController = FindFirstObjectByType<PlayerController2D>();

            UpdateCurrentItem();
        }
        private void Start()
        {
            for (int i = 0; i < _cells.Length; i++)
            {
                _cells[i].ItemStack = GameItems.GetItem(0).Clone();
                UpdateCellUI(i);
            }
            _draggedItem = GameItems.GetItem(0);
            ItemStack testcell = GameItems.GetItem(1);
            testcell.Count = 128;
            AddItem(testcell);
            testcell.Count = 12;
            AddItem(testcell);
            testcell.Count = 22;
            AddItem(testcell);
            ItemStack testcell2 = GameItems.GetItem(2);
            testcell2.Count = 128;
            AddItem(testcell2);
        }

        private void Update()
        {
            if (_inputManager.UI.ToggleInventory.WasPressedThisFrame())
            {
                ToggleInventory();
            }
            if (_inputManager.UI.ThrowObject.WasPressedThisFrame())
            {
                ThrowObject();
            }

            if (_isInventoryOpen)
            {
                HandleMouseInteraction();
            }
            else
            {
                HandleHotbarNavigation();
            }
        }

        private void ThrowObject()
        {
            GameObject obj = Instantiate(_thrownObject);
            obj.transform.position = gameObject.transform.position + new Vector3(0, 1.5f, 0);
            obj.GetComponent<ThrownObjectController>().SetItem(_cells[_currentCell].ItemStack);
            obj.GetComponent<SpriteRenderer>().sprite = GameItems.GetItem(_currentCell).Item.icon;
            obj.GetComponent<Rigidbody2D>().linearVelocity = new Vector2(gameObject.transform.lossyScale.x * 4, 2);
            RemoveItem(_currentCell);
        }

        private void ToggleInventory()
        {
            _isInventoryOpen = !_isInventoryOpen;
            _inventoryUI.SetActive(_isInventoryOpen);

            if (_isInventoryOpen)
            {
                _inputManager.Player.Disable();
                _playerController.DisableInput();
            }
            else
            {
                _inputManager.Player.Enable();
                _playerController.EnableInput();

                for (int i = 0; i < _cells.Length; i++)
                {
                    _cells[i].ItemContainer.GetComponent<Image>().color = Color.white;
                }
            }
        }

        private void HandleMouseInteraction()
        {
            Vector2 mousePos = Mouse.current.position.ReadValue();
            HighlightSlots(mousePos);

            if (_isDraggingItem)
            {
                _draggedItemObject.transform.position = mousePos;

                if (Mouse.current.leftButton.wasReleasedThisFrame)
                {
                    if (!_isFirstClick)
                    {
                        TryPlaceItemInSlot();
                    }
                    _isFirstClick = false; // Сброс флага после обработки
                }
                else if (Mouse.current.rightButton.wasReleasedThisFrame)
                {
                    TryPlaceItemInSlotRightClick();
                }
            }
            else
            {
                if (Mouse.current.leftButton.wasPressedThisFrame)
                {
                    _isFirstClick = true;
                    CheckSlotForDrag(mousePos);
                }
                else if (Mouse.current.rightButton.wasReleasedThisFrame)
                {
                    CheckSlotForHalfDrag(mousePos);
                }
            }
        }

        private void HighlightSlots(Vector2 mousePos)
        {
            for (int i = 0; i < _cells.Length; i++)
            {
                if (RectTransformUtility.RectangleContainsScreenPoint(_cells[i].ItemContainer.GetComponent<RectTransform>(), mousePos))
                {
                    _cells[i].ItemContainer.GetComponent<Image>().color = Color.blue;
                }
                else
                {
                    _cells[i].ItemContainer.GetComponent<Image>().color = Color.white;
                }
            }
        }

        private void TryPlaceItemInSlotRightClick()
        {
            for (int i = 0; i < _cells.Length; i++)
            {
                if (RectTransformUtility.RectangleContainsScreenPoint(_cells[i].ItemContainer.GetComponent<RectTransform>(), Mouse.current.position.ReadValue()))
                {
                    if (_cells[i].ItemStack.Item.id == _draggedItem.Item.id && _cells[i].ItemStack.Count + 1 <= _cells[i].ItemStack.Item.MaxCount)
                    {
                        _cells[i].ItemStack.Count += 1;
                        UpdateCellUI(i);
                        if (_draggedItem.Count == 1)
                        {
                            Destroy(_draggedItemObject);
                            _isDraggingItem = false;
                        }
                        else
                        {
                            _draggedItem.Count -= 1;
                            _draggedItemObject.GetComponentInChildren<TMP_Text>().text = _draggedItem.Count.ToString();
                        }
                    }
                    else if(_cells[i].ItemStack.Item.id == 0)
                    {
                        ItemStack newItem = _draggedItem.Clone();
                        newItem.Count = 1;
                        AddItem(newItem, i);
                        if (_draggedItem.Count == 1)
                        {
                            Destroy(_draggedItemObject);
                            _isDraggingItem = false;
                        }
                        else
                        {
                            _draggedItem.Count -= 1;
                            _draggedItemObject.GetComponentInChildren<TMP_Text>().text = _draggedItem.Count.ToString();
                        }
                    }
                    else
                    {
                        // Если предметы разные, меняем местами
                        SwapItemsInSlot(_draggedItem, i);
                    }
                    return;
                }
            }

            // Если не попали в ячейку, просто убираем иконку
            Destroy(_draggedItemObject);
            _isDraggingItem = false;
        }

        private void TryPlaceItemInSlot()
        {
            for (int i = 0; i < _cells.Length; i++)
            {
                if (RectTransformUtility.RectangleContainsScreenPoint(_cells[i].ItemContainer.GetComponent<RectTransform>(), Mouse.current.position.ReadValue()))
                {
                    if (_cells[i].ItemStack.Item.id == _draggedItem.Item.id && _cells[i].ItemStack.Count + _draggedItem.Count <= _cells[i].ItemStack.Item.MaxCount)
                    {
                        _cells[i].ItemStack.Count += _draggedItem.Count;
                        UpdateCellUI(i);
                        Destroy(_draggedItemObject);
                        _isDraggingItem = false;
                    }
                    else
                    {
                        // Если предметы разные, меняем местами
                        SwapItemsInSlot(_draggedItem, i);
                    }
                    return;
                }
            }

            // Если не попали в ячейку, просто убираем иконку
            Destroy(_draggedItemObject);
            _isDraggingItem = false;
        }

        private void CheckSlotForDrag(Vector2 mousePos)
        {
            for (int i = 0; i < _cells.Length; i++)
            {
                Debug.Log(mousePos);
                if (RectTransformUtility.RectangleContainsScreenPoint(_cells[i].ItemContainer.GetComponent<RectTransform>(), mousePos) && _cells[i].ItemStack.Item.id != 0)
                {
                    StartDraggingItem(_cells[i].ItemStack, i);
                    RemoveItem(i);
                    return;
                }
            }
        }
        private void CheckSlotForHalfDrag(Vector2 mousePos)
        {
            for (int i = 0; i < _cells.Length; i++)
            {
                Debug.Log(mousePos);
                if (RectTransformUtility.RectangleContainsScreenPoint(_cells[i].ItemContainer.GetComponent<RectTransform>(), mousePos) && _cells[i].ItemStack.Item.id != 0)
                {
                    if(_cells[i].ItemStack.Count == 1)
                    {
                        StartDraggingItem(_cells[i].ItemStack, i);
                        RemoveItem(i);
                        return;
                    }
                    ItemStack newitem = _cells[i].ItemStack.Clone();
                    newitem.Count = (int)Math.Floor(newitem.Count / 2f);
                    StartDraggingItem(newitem);
                    _cells[i].ItemStack.Count -= newitem.Count;
                    UpdateCellUI(i);
                    return;
                }
            }
        }

        private void StartDraggingItem(ItemStack item, int? cell = null)
        {
            _draggedItem = item.Clone();
            // Создаем объект для перетаскивания
            _draggedItemObject = new GameObject("DraggedItemIcon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));

            // Логика для ячейки, если она не равна null
            if (cell != null)
            {
                if (_cells[cell.Value].ItemCountText != null)
                {
                    _draggedItem.Count = _cells[cell.Value].ItemStack.Count;
                    Instantiate(_countUIText, _draggedItemObject.transform).GetComponent<TMP_Text>().text = _draggedItem.Count.ToString();
                }
                else
                {
                    _draggedItem.Count = _cells[cell.Value].ItemStack.Count;
                }
            }
            else
            {
                // Если cell == null, просто присваиваем количество из текущего элемента
                _draggedItem.Count = item.Count;

                // Создаем текст с количеством, если это нужно даже без ячейки
                Instantiate(_countUIText, _draggedItemObject.transform).GetComponent<TMP_Text>().text = _draggedItem.Count.ToString();
            }


            Debug.Log(_draggedItem.Item.id);
            // Настройка перетаскиваемого объекта
            _draggedItemObject.GetComponent<Image>().preserveAspect = true;
            _draggedItemObject.transform.SetParent(_inventoryUI.transform);
            _draggedItemObject.GetComponent<Image>().sprite = _draggedItem.Item.icon;
            _draggedItemObject.GetComponent<RectTransform>().sizeDelta = new Vector2(100, 100);
            _draggedItemObject.transform.SetAsLastSibling();
            _isDraggingItem = true;
        }

        public void AddItem(ItemStack item, int? inventoryCell = null)
        {
            if (inventoryCell != null)
            {
                PlaceItemInSlot(item, inventoryCell.Value);
            }
            else
            {
                int slot = FindFirstSlot(item.Item.id);
                if (slot != -1)
                {
                    PlaceItemInSlot(item, slot);
                }
                else
                {
                    slot = FindFirstSlot();
                    if (slot != -1)
                    {
                        PlaceItemInSlot(item, slot);
                    }
                    else
                    {
                        Debug.LogWarning("Инвентарь полон!");
                    }
                }
            }
        }
        public ItemStack GetInventoryItem(int inventoryCell)
        {
            return _cells[inventoryCell].ItemStack;
        }


        public int GetCurrentCell()
        {
            return _currentCell;
        }

        public int FindFirstSlot(int? slot = 0)
        {
            for (int i = 0; i < _cells.Length; i++)
            {
                Debug.Log(_cells[i].ItemStack.Item.id);
                if (_cells[i].ItemStack.Item.id == slot && _cells[i].ItemStack.Count < _cells[i].ItemStack.Item.MaxCount)
                    return i;
            }
            return -1;
        }
        public int FindFirstSlotOfItemType(ItemType n)
        {
            for (int i = 0; i < _cells.Length; i++)
            {
                if (GameItems.GetItem(i).Item.type == n)
                    return i;
            }
            return -1;
        }

        private void PlaceItemInSlot(ItemStack item, int inventoryCell)
        {
            int maxCount = item.Item.MaxCount; // �������� ������������ ���������� ��� ������� ��������

            // ���������, ����� �� ���������� �������� � ������� ������
            if (_cells[inventoryCell].ItemStack.Item.id == item.Item.id && _cells[inventoryCell].ItemStack.Item.name == item.Item.name && _cells[inventoryCell].ItemStack.Item.description == item.Item.description)
            {
                int availableSpace = maxCount - _cells[inventoryCell].ItemStack.Count;
                Debug.Log(availableSpace + " " + item.Count + " " + _cells[inventoryCell].ItemStack.Count);
                if (item.Count <= availableSpace)
                {
                    _cells[inventoryCell].ItemStack.Count += item.Count;
                    UpdateCellUI(inventoryCell);
                }
                else
                {
                    _cells[inventoryCell].ItemStack.Count = maxCount;
                    item.Count -= availableSpace;
                    if (item.Count > 0)
                    {
                        PlaceItemInFirstEmptySlot(item); // �������� ���������� �������� � ������ ������ ������
                    }
                    UpdateCellUI(inventoryCell);
                }
            }
            else
            {
                _cells[inventoryCell].ItemStack = item.Clone();
                item.Count -= _cells[inventoryCell].ItemStack.Count;
                if (item.Count > 0)
                {   
                    PlaceItemInFirstEmptySlot(item); // �������� ���������� �������� � ������ ������ ������
                }
                UpdateCellUI(inventoryCell);
            }

            OnItemUpdated?.Invoke(_cells[inventoryCell].ItemStack, inventoryCell);
        }
        private void UpdateCellUI(int cellIndex)
        {
            // Обновление изображения предмета
            if (_cells[cellIndex].ItemStack.Item.icon != null)
            {
                if (_cells[cellIndex].ItemSprite == null)
                {
                    _cells[cellIndex].ItemSprite = Instantiate(_imageUI, _cells[cellIndex].ItemContainer.transform).GetComponent<Image>();
                }
                _cells[cellIndex].ItemSprite.sprite = _cells[cellIndex].ItemStack.Item.icon;
            }
            else
            {
                if (_cells[cellIndex].ItemSprite != null)
                {
                    Destroy(_cells[cellIndex].ItemSprite.gameObject);
                    _cells[cellIndex].ItemSprite = null;
                }
            }

            // Обновление текста количества предметов
            if (_cells[cellIndex].ItemStack.Count > 1)
            {
                if (_cells[cellIndex].ItemCountText == null)
                {
                    _cells[cellIndex].ItemCountText = Instantiate(_countUIText, _cells[cellIndex].ItemContainer.transform).GetComponent<TMP_Text>();
                }
                _cells[cellIndex].ItemCountText.text = _cells[cellIndex].ItemStack.Count.ToString();
            }
            else
            {
                if (_cells[cellIndex].ItemCountText != null)
                {
                    Destroy(_cells[cellIndex].ItemCountText.gameObject);
                    _cells[cellIndex].ItemCountText = null;
                }
            }
        }

        private void PlaceItemInFirstEmptySlot(ItemStack item)
        {
            int slot = FindFirstSlot();
            if (slot != -1)
            {
                PlaceItemInSlot(item, slot);
            }
            else
            {
                Debug.LogWarning("��������� �����! �� ������� ���������� ���������� ��������.");
            }
        }
        public void RemoveItem(int inventoryCell, int? countRemove = null)
        {
            void SetItemToPlaceholder(int cellIndex)
            {
                _cells[cellIndex].ItemStack = GameItems.GetItem(0);
                UpdateCellUI(cellIndex);
            }

            if (inventoryCell < _hotbarSize)
            {
                if (countRemove == null || countRemove.Value >= _cells[inventoryCell].ItemStack.Count)
                {
                    SetItemToPlaceholder(inventoryCell);
                }
                else
                {
                    _cells[inventoryCell].ItemStack.Count -= countRemove.Value;
                    UpdateCellUI(inventoryCell);
                }
            }
            else
            {
                int mainInventoryIndex = inventoryCell - _hotbarSize;
                if (countRemove == null || countRemove.Value >= _cells[_hotbarSize + mainInventoryIndex].ItemStack.Count)
                {
                    SetItemToPlaceholder(_hotbarSize + mainInventoryIndex);
                }
                else
                {
                    _cells[_hotbarSize + mainInventoryIndex].ItemStack.Count -= countRemove.Value;
                    UpdateCellUI(_hotbarSize + mainInventoryIndex);
                }
            }

            OnItemUpdated?.Invoke(new ItemStack(), inventoryCell);
        }
        private void SwapItemsInSlot(ItemStack item, int targetCell)
        {
            // Сохраняем предмет из целевой ячейки
            ItemStack targetItem = _cells[targetCell].ItemStack.Clone();

            // Если целевая ячейка не пуста
            if (targetItem.Item.id != 0)
            {
                Debug.Log($"Нужно поменять местами предмет с id {targetItem.Item.id} и предмет с id {item.Item.id}");

                // Удаляем объект с изображением перетаскиваемого предмета
                Destroy(_draggedItemObject);

                // Инициализируем перетаскивание для старого предмета (передаем cell)
                StartDraggingItem(targetItem);

                // Удаляем старый предмет из целевой ячейки
                RemoveItem(targetCell);

                // Добавляем новый предмет в целевую ячейку
                AddItem(item, targetCell);

                // Обновляем интерфейс ячейки
                UpdateCellUI(targetCell);
            }
            else
            {
                // Если целевая ячейка пуста, просто добавляем новый предмет
                AddItem(item, targetCell);
                Destroy(_draggedItemObject);
                _isDraggingItem = false;

                // Обновляем интерфейс ячейки
                UpdateCellUI(targetCell);
            }
        }

        private void HandleHotbarNavigation()
        {
            // ������������� �������� ���� ��� ������������ ������
            float scrollValue = Input.GetAxis("Mouse ScrollWheel");

            if (scrollValue < 0)
            {
                _currentCell = (_currentCell + 1) % _hotbarSize;
                UpdateCurrentItem();
                OnItemUpdated?.Invoke(GetInventoryItem(_currentCell), _currentCell);
            }
            else if (scrollValue > 0)
            {
                _currentCell = (_currentCell - 1 + _hotbarSize) % _hotbarSize;
                UpdateCurrentItem();
                OnItemUpdated?.Invoke(GetInventoryItem(_currentCell), _currentCell);
            }
        }

        private void UpdateCurrentItem()
        {
            _cellHighlight.GetComponent<RectTransform>().position = _cells[_currentCell].ItemContainer.GetComponent<RectTransform>().position;
        }

        public void EnableInput()
        {
            _inputManager.UI.Enable();
        }

        public void DisableInput()
        {
            _inputManager.UI.Disable();
        }
    }
}