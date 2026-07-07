using UnityEngine;
using UnityEngine.UI;

namespace UIWidgets
{
public class CardExpanding2D : MonoBehaviour
{

    [SerializeField]
    private float lerpSpeed = 8f;

    [SerializeField]
    private RectTransform buttonRect = null;
    private Vector2 closeButtonMin = Vector2.zero;
    private Vector2 closeButtonMax = Vector2.zero;

    [SerializeField]
    private Vector2 cardSize = Vector2.zero;
    [SerializeField]
    private Vector2 pageSize = Vector2.zero;

    private Vector2 cardCenter = Vector2.zero;
    private Vector2 pageCenter = Vector2.zero;

    private Vector2 cardMin = Vector2.zero;
    private Vector2 cardMax = Vector2.zero;
    private Vector2 pageMin = Vector2.zero;
    private Vector2 pageMax = Vector2.zero;

    private RectTransform rectTrans;
    private Image buttonImage;
    ///I wouldn't recommend changing animationActive's value here unless you want the card to start as a page.
    private int animationActive = -1;

    void Start()
    {
        rectTrans = GetComponent<RectTransform>();
        buttonImage = buttonRect.GetComponent<Image>();

        ///Setting up the button's starting color and page position.
        buttonImage.color = new Color32(228, 0, 0, 0);

        ///Setting up the card and page offsets.
        cardMin = new Vector2(cardCenter.x - cardSize.x * 0.5f, cardCenter.y - cardSize.y * 0.5f);
        cardMax = new Vector2(cardCenter.x + cardSize.x * 0.5f, cardCenter.y + cardSize.y * 0.5f);

        pageMin = new Vector2(pageCenter.x - pageSize.x * 0.5f, pageCenter.y - pageSize.y * 0.5f);
        pageMax = new Vector2(pageCenter.x + pageSize.x * 0.5f, pageCenter.y + pageSize.y * 0.5f);

        closeButtonMin = new Vector2(pageMin.x + pageSize.x - 64, pageMin.y + pageSize.y - 64);
        closeButtonMax = new Vector2(pageMax.x - 16, pageMax.y - 16);
    }

    void Update()
    {
        ///When animationActive == 1, the card is expanding into a page.
        if (animationActive == 1)
        {
            rectTrans.offsetMin = Vector2.Lerp(rectTrans.offsetMin, pageMin, Time.deltaTime * lerpSpeed);
            rectTrans.offsetMax = Vector2.Lerp(rectTrans.offsetMax, pageMax, Time.deltaTime * lerpSpeed);

            if (rectTrans.offsetMin.x < pageMin.x * 0.995f && rectTrans.offsetMin.y < pageMin.y * 0.995f && rectTrans.offsetMax.x > pageMax.x * 0.995f && rectTrans.offsetMax.y > pageMax.y * 0.995f)
            {
                rectTrans.offsetMin = pageMin;
                rectTrans.offsetMax = pageMax;

                ///Changes the button color so it's visible in the page view.
                buttonImage.color = Color32.Lerp(buttonImage.color, new Color32(228, 0, 0, 191), Time.deltaTime * lerpSpeed);

                if (Mathf.Abs(buttonImage.color.a - 191) < 2)
                {
                    buttonImage.color = new Color32(228, 0, 0, 191);

                    animationActive = 0;
                    CardStack2D.canUseHorizontalAxis = true;
                }
            }
            ///When animationActive == -1, the page is shrinking into a card.
        }
        else if (animationActive == -1)
        {
            buttonImage.color = Color32.Lerp(buttonImage.color, new Color32(228, 0, 0, 0), Time.deltaTime * lerpSpeed * 1.25f);

            rectTrans.offsetMin = Vector2.Lerp(rectTrans.offsetMin, cardMin, Time.deltaTime * lerpSpeed);
            rectTrans.offsetMax = Vector2.Lerp(rectTrans.offsetMax, cardMax, Time.deltaTime * lerpSpeed);

            if (rectTrans.offsetMin.x > cardMin.x * 1.005f && rectTrans.offsetMin.y > cardMin.y * 1.005f && rectTrans.offsetMax.x < cardMax.x * 1.005f && rectTrans.offsetMax.y < cardMax.y * 1.005f)
            {
                rectTrans.offsetMin = cardMin;
                rectTrans.offsetMax = cardMax;

                ///Makes the button take up the whole card.
                buttonRect.offsetMin = Vector2.zero;
                buttonRect.offsetMax = Vector2.zero;

                animationActive = 0;
                CardStack2D.canUseHorizontalAxis = true;
            }
        }
    }

    public void ToggleCard()
    {
        CardStack2D.canUseHorizontalAxis = false;
        if (animationActive != 1)
        {
            animationActive = 1;
            cardCenter = transform.localPosition;

            ///Makes the button the right size in page view.
            buttonRect.offsetMin = closeButtonMin;
            buttonRect.offsetMax = closeButtonMax;
        }
        else if (animationActive != -1)
        {
            animationActive = -1;
        }
    }
}
}