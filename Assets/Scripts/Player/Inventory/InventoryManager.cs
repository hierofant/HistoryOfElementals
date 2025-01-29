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
            public ItemStack itemStack;
            public RectTransform itemContainer = null;
            public TMP_Text itemCountText = null;
            public Image itemSprite = null;
        }
        [SerializeField] private RectTransform[] CellsUI; // ������ ��������� (������� � �������� ������)
        [SerializeField] private GameObject cellHighlight; // ��������� ������� ������
        [SerializeField] private GameObject inventoryUI; // ������ ���������
        [SerializeField] private GameObject countUIText; // ����� ����������
        [SerializeField] private GameObject imageUI; // ����� ����������
        [SerializeField] private GameObject thrownObject; // ������ ������������ ��������
        [SerializeField] private int currentCell = 0; // ������� ��������� ������ � ������� ������
        [SerializeField] private int hotbarSize = 5; // ���������� ������ � ������� ����
        private InventoryItemCell[] Cells; // ��� ����� ��������� (� ������� ���, � �������� ���������)
        private InputManager inputManager; // ���������� ������
        private GameItems gameItems; // ������ �� ����� � ����������
        private bool isInventoryOpen = false; // ��������� ���������
        private bool isDraggingItem = false; // ������ �������������� ��������
        private GameObject draggedItemObject; // ������ ���������������� ��������
        private ItemStack draggedItem;
        private PlayerController2D playerController; // ��� ���������� �������� ��� �������� ���������
        private bool isFirstClick;

        public static event Action<ItemStack, int> OnItemUpdated; // ������� ��� ���������� �������� � ������

        private void Awake()
        {
            Cells = new InventoryItemCell[CellsUI.Length];
            for (int i = 0; i < Cells.Length; i++)
            {
                Cells[i] = new InventoryItemCell();
            }
            for (int i = 0; i < Cells.Length; i++)
            {
                Cells[i].itemContainer = CellsUI[i];
                Cells[i].itemCountText = CellsUI[i].GetComponentInChildren<TMP_Text>();
            }

            cellHighlight.GetComponent<RectTransform>().position = CellsUI[currentCell].GetComponent<RectTransform>().position;

            inputManager = new InputManager();
            inputManager.UI.Enable();
            gameItems = GetComponent<GameItems>();
            playerController = FindFirstObjectByType<PlayerController2D>();

            UpdateCurrentItem();
        }
        private void Start()
        {
            for (int i = 0; i < Cells.Length; i++)
            {
                Cells[i].itemStack = GameItems.GetItem(0).Clone();
                UpdateCellUI(i);
            }
            draggedItem = GameItems.GetItem(0);
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
            if (inputManager.UI.ToggleInventory.WasPressedThisFrame())
            {
                ToggleInventory();
            }
            if (inputManager.UI.ThrowObject.WasPressedThisFrame())
            {
                ThrowObject();
            }

            if (isInventoryOpen)
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
            GameObject obj = Instantiate(thrownObject);
            obj.transform.position = gameObject.transform.position + new Vector3(0, 1.5f, 0);
            obj.GetComponent<ThrownObjectController>().SetItem(Cells[currentCell].itemStack);
            obj.GetComponent<SpriteRenderer>().sprite = GameItems.GetItem(currentCell).Item.icon;
            obj.GetComponent<Rigidbody2D>().linearVelocity = new Vector2(gameObject.transform.lossyScale.x * 4, 2);
            RemoveItem(currentCell);
        }

        private void ToggleInventory()
        {
            isInventoryOpen = !isInventoryOpen;
            inventoryUI.SetActive(isInventoryOpen);

            if (isInventoryOpen)
            {
                inputManager.Player.Disable();
                playerController.DisableInput();
            }
            else
            {
                inputManager.Player.Enable();
                playerController.EnableInput();

                for (int i = 0; i < Cells.Length; i++)
                {
                    Cells[i].itemContainer.GetComponent<Image>().color = Color.white;
                }
            }
        }

        private void HandleMouseInteraction()
        {
            Vector2 mousePos = Mouse.current.position.ReadValue();
            HighlightSlots(mousePos);

            if (isDraggingItem)
            {
                draggedItemObject.transform.position = mousePos;

                if (Mouse.current.leftButton.wasReleasedThisFrame)
                {
                    if (!isFirstClick)
                    {
                        TryPlaceItemInSlot();
                    }
                    isFirstClick = false; // Сброс флага после обработки
                }
                else if (Mouse.current.rightButton.wasReleasedThisFrame)
                {
                    TryPlaceItemInSlotRightButton();
                }
            }
            else
            {
                if (Mouse.current.leftButton.wasPressedThisFrame)
                {
                    isFirstClick = true;
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
            for (int i = 0; i < Cells.Length; i++)
            {
                if (RectTransformUtility.RectangleContainsScreenPoint(Cells[i].itemContainer.GetComponent<RectTransform>(), mousePos))
                {
                    Cells[i].itemContainer.GetComponent<Image>().color = Color.blue;
                }
                else
                {
                    Cells[i].itemContainer.GetComponent<Image>().color = Color.white;
                }
            }
        }

        private void TryPlaceItemInSlotRightButton()
        {
            for (int i = 0; i < Cells.Length; i++)
            {
                if (RectTransformUtility.RectangleContainsScreenPoint(Cells[i].itemContainer.GetComponent<RectTransform>(), Mouse.current.position.ReadValue()))
                {
                    if (Cells[i].itemStack.Item.id == draggedItem.Item.id && Cells[i].itemStack.Count + 1 <= Cells[i].itemStack.Item.MaxCount)
                    {
                        Cells[i].itemStack.Count += 1;
                        UpdateCellUI(i);
                        if (draggedItem.Count == 1)
                        {
                            Destroy(draggedItemObject);
                            isDraggingItem = false;
                        }
                        else
                        {
                            draggedItem.Count -= 1;
                            draggedItemObject.GetComponentInChildren<TMP_Text>().text = draggedItem.Count.ToString();
                        }
                    }
                    else if(Cells[i].itemStack.Item.id == 0)
                    {
                        ItemStack newItem = draggedItem.Clone();
                        newItem.Count = 1;
                        AddItem(newItem, i);
                        if (draggedItem.Count == 1)
                        {
                            Destroy(draggedItemObject);
                            isDraggingItem = false;
                        }
                        else
                        {
                            draggedItem.Count -= 1;
                            draggedItemObject.GetComponentInChildren<TMP_Text>().text = draggedItem.Count.ToString();
                        }
                    }
                    else
                    {
                        // Если предметы разные, меняем местами
                        SwapItemsInSlot(draggedItem, i);
                    }
                    return;
                }
            }

            // Если не попали в ячейку, просто убираем иконку
            Destroy(draggedItemObject);
            isDraggingItem = false;
        }

        private void TryPlaceItemInSlot()
        {
            for (int i = 0; i < Cells.Length; i++)
            {
                if (RectTransformUtility.RectangleContainsScreenPoint(Cells[i].itemContainer.GetComponent<RectTransform>(), Mouse.current.position.ReadValue()))
                {
                    if (Cells[i].itemStack.Item.id == draggedItem.Item.id && Cells[i].itemStack.Count + draggedItem.Count <= Cells[i].itemStack.Item.MaxCount)
                    {
                        Cells[i].itemStack.Count += draggedItem.Count;
                        UpdateCellUI(i);
                        Destroy(draggedItemObject);
                        isDraggingItem = false;
                    }
                    else
                    {
                        // Если предметы разные, меняем местами
                        SwapItemsInSlot(draggedItem, i);
                    }
                    return;
                }
            }

            // Если не попали в ячейку, просто убираем иконку
            Destroy(draggedItemObject);
            isDraggingItem = false;
        }

        private void CheckSlotForDrag(Vector2 mousePos)
        {
            for (int i = 0; i < Cells.Length; i++)
            {
                Debug.Log(mousePos);
                if (RectTransformUtility.RectangleContainsScreenPoint(Cells[i].itemContainer.GetComponent<RectTransform>(), mousePos) && Cells[i].itemStack.Item.id != 0)
                {
                    StartDraggingItem(Cells[i].itemStack, i);
                    RemoveItem(i);
                    return;
                }
            }
        }
        private void CheckSlotForHalfDrag(Vector2 mousePos)
        {
            for (int i = 0; i < Cells.Length; i++)
            {
                Debug.Log(mousePos);
                if (RectTransformUtility.RectangleContainsScreenPoint(Cells[i].itemContainer.GetComponent<RectTransform>(), mousePos) && Cells[i].itemStack.Item.id != 0)
                {
                    if(Cells[i].itemStack.Count == 1)
                    {
                        StartDraggingItem(Cells[i].itemStack, i);
                        RemoveItem(i);
                        return;
                    }
                    ItemStack newitem = Cells[i].itemStack.Clone();
                    newitem.Count = (int)Math.Floor(newitem.Count / 2f);
                    StartDraggingItem(newitem);
                    Cells[i].itemStack.Count -= newitem.Count;
                    UpdateCellUI(i);
                    return;
                }
            }
        }

        private void StartDraggingItem(ItemStack item, int? cell = null)
        {
            draggedItem = item.Clone();
            // Создаем объект для перетаскивания
            draggedItemObject = new GameObject("DraggedItemIcon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));

            // Логика для ячейки, если она не равна null
            if (cell != null)
            {
                if (Cells[cell.Value].itemCountText != null)
                {
                    draggedItem.Count = Cells[cell.Value].itemStack.Count;
                    Instantiate(countUIText, draggedItemObject.transform).GetComponent<TMP_Text>().text = draggedItem.Count.ToString();
                }
                else
                {
                    draggedItem.Count = Cells[cell.Value].itemStack.Count;
                }
            }
            else
            {
                // Если cell == null, просто присваиваем количество из текущего элемента
                draggedItem.Count = item.Count;

                // Создаем текст с количеством, если это нужно даже без ячейки
                Instantiate(countUIText, draggedItemObject.transform).GetComponent<TMP_Text>().text = draggedItem.Count.ToString();
            }


            Debug.Log(draggedItem.Item.id);
            // Настройка перетаскиваемого объекта
            draggedItemObject.GetComponent<Image>().preserveAspect = true;
            draggedItemObject.transform.SetParent(inventoryUI.transform);
            draggedItemObject.GetComponent<Image>().sprite = draggedItem.Item.icon;
            draggedItemObject.GetComponent<RectTransform>().sizeDelta = new Vector2(100, 100);
            draggedItemObject.transform.SetAsLastSibling();
            isDraggingItem = true;
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
            return Cells[inventoryCell].itemStack;
        }


        public int GetCurrentCell()
        {
            return currentCell;
        }

        public int FindFirstSlot(int? slot = 0)
        {
            for (int i = 0; i < Cells.Length; i++)
            {
                Debug.Log(Cells[i].itemStack.Item.id);
                if (Cells[i].itemStack.Item.id == slot && Cells[i].itemStack.Count < Cells[i].itemStack.Item.MaxCount)
                    return i;
            }
            return -1;
        }
        public int FindFirstSlotOfItemType(ItemType n)
        {
            for (int i = 0; i < Cells.Length; i++)
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
            if (Cells[inventoryCell].itemStack.Item.id == item.Item.id && Cells[inventoryCell].itemStack.Item.name == item.Item.name && Cells[inventoryCell].itemStack.Item.description == item.Item.description)
            {
                int availableSpace = maxCount - Cells[inventoryCell].itemStack.Count;
                Debug.Log(availableSpace + " " + item.Count + " " + Cells[inventoryCell].itemStack.Count);
                if (item.Count <= availableSpace)
                {
                    Cells[inventoryCell].itemStack.Count += item.Count;
                    UpdateCellUI(inventoryCell);
                }
                else
                {
                    Cells[inventoryCell].itemStack.Count = maxCount;
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
                Cells[inventoryCell].itemStack = item.Clone();
                item.Count -= Cells[inventoryCell].itemStack.Count;
                if (item.Count > 0)
                {   
                    PlaceItemInFirstEmptySlot(item); // �������� ���������� �������� � ������ ������ ������
                }
                UpdateCellUI(inventoryCell);
            }

            OnItemUpdated?.Invoke(Cells[inventoryCell].itemStack, inventoryCell);
        }
        private void UpdateCellUI(int cellIndex)
        {
            // Обновление изображения предмета
            if (Cells[cellIndex].itemStack.Item.icon != null)
            {
                if (Cells[cellIndex].itemSprite == null)
                {
                    Cells[cellIndex].itemSprite = Instantiate(imageUI, Cells[cellIndex].itemContainer.transform).GetComponent<Image>();
                }
                Cells[cellIndex].itemSprite.sprite = Cells[cellIndex].itemStack.Item.icon;
            }
            else
            {
                if (Cells[cellIndex].itemSprite != null)
                {
                    Destroy(Cells[cellIndex].itemSprite.gameObject);
                    Cells[cellIndex].itemSprite = null;
                }
            }

            // Обновление текста количества предметов
            if (Cells[cellIndex].itemStack.Count > 1)
            {
                if (Cells[cellIndex].itemCountText == null)
                {
                    Cells[cellIndex].itemCountText = Instantiate(countUIText, Cells[cellIndex].itemContainer.transform).GetComponent<TMP_Text>();
                }
                Cells[cellIndex].itemCountText.text = Cells[cellIndex].itemStack.Count.ToString();
            }
            else
            {
                if (Cells[cellIndex].itemCountText != null)
                {
                    Destroy(Cells[cellIndex].itemCountText.gameObject);
                    Cells[cellIndex].itemCountText = null;
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
                Cells[cellIndex].itemStack = GameItems.GetItem(0);
                UpdateCellUI(cellIndex);
            }

            if (inventoryCell < hotbarSize)
            {
                if (countRemove == null || countRemove.Value >= Cells[inventoryCell].itemStack.Count)
                {
                    SetItemToPlaceholder(inventoryCell);
                }
                else
                {
                    Cells[inventoryCell].itemStack.Count -= countRemove.Value;
                    UpdateCellUI(inventoryCell);
                }
            }
            else
            {
                int mainInventoryIndex = inventoryCell - hotbarSize;
                if (countRemove == null || countRemove.Value >= Cells[hotbarSize + mainInventoryIndex].itemStack.Count)
                {
                    SetItemToPlaceholder(hotbarSize + mainInventoryIndex);
                }
                else
                {
                    Cells[hotbarSize + mainInventoryIndex].itemStack.Count -= countRemove.Value;
                    UpdateCellUI(hotbarSize + mainInventoryIndex);
                }
            }

            OnItemUpdated?.Invoke(new ItemStack(), inventoryCell);
        }

        public void RemoveItemToConsole(int itemID, int? itemCount = null, ItemType? l = null)
        {
            int slot = 0;
            if (l != null)
            {
                slot = FindFirstSlotOfItemType(l.Value);
            }
            else
            {
                slot = FindFirstSlot(itemID);
            }
            if (itemCount == null)
            {
                RemoveItem(slot);
            }
            else
            {
                RemoveItem(slot, itemCount);
            }
        }
        public void RemoveItemToConsoleOfItemType(ItemType l, int? itemCount = null)
        {
            int slot = FindFirstSlotOfItemType(l);

            if (itemCount == null)
            {
                RemoveItem(slot);
            }
            else
            {
                RemoveItem(slot, itemCount);
            }
        }
        private void SwapItemsInSlot(ItemStack item, int targetCell)
        {
            // Сохраняем предмет из целевой ячейки
            ItemStack targetItem = Cells[targetCell].itemStack.Clone();

            // Если целевая ячейка не пуста
            if (targetItem.Item.id != 0)
            {
                Debug.Log($"Нужно поменять местами предмет с id {targetItem.Item.id} и предмет с id {item.Item.id}");

                // Удаляем объект с изображением перетаскиваемого предмета
                Destroy(draggedItemObject);

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
                Destroy(draggedItemObject);
                isDraggingItem = false;

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
                currentCell = (currentCell + 1) % hotbarSize;
                UpdateCurrentItem();
                OnItemUpdated?.Invoke(GetInventoryItem(currentCell), currentCell);
            }
            else if (scrollValue > 0)
            {
                currentCell = (currentCell - 1 + hotbarSize) % hotbarSize;
                UpdateCurrentItem();
                OnItemUpdated?.Invoke(GetInventoryItem(currentCell), currentCell);
            }
        }

        private void UpdateCurrentItem()
        {
            cellHighlight.GetComponent<RectTransform>().position = Cells[currentCell].itemContainer.GetComponent<RectTransform>().position;
        }

        public void EnableInput()
        {
            inputManager.UI.Enable();
        }

        public void DisableInput()
        {
            inputManager.UI.Disable();
        }
    }

}