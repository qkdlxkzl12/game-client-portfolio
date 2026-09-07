using UnityEngine;
using TB.GameState;

public class ClickManager : StateSingleton<ClickManager>
{
    private void Awake()
    {
        Initialize(GameState.Ingame);
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Camera cam = Camera.main;
            Ray ray = cam.ScreenPointToRay(Input.mousePosition);

            // 2D 물리에서 3D ray와 교차 검사
            RaycastHit2D hit = Physics2D.GetRayIntersection(ray);

            if (hit.collider != null)
            {
                IClickable touchable = hit.collider.GetComponent<IClickable>();
                if (touchable != null && touchable.IsClickable)
                {
                    touchable.OnClick();
                }
            }
        }
    }
}
