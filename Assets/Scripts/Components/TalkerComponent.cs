using UnityEngine;
using UnityEngine.UI;

public class TalkerComponent : MonoBehaviour
{
    public GameObject talker;
    public Text talkerText;
    public float disappearDelay;
    public float remainTime;
    public bool runTimer;
    public Vector3 offset;

    // Update is called once per frame
    void Start()
    {
        talkerText = talkerText.GetComponent<Text>();
        remainTime = disappearDelay;
    }

    public void Update()
    {
        talkerText.transform.localPosition = talker.transform.position + offset;
        if (runTimer)
        {
            remainTime -= Time.deltaTime;
            if (remainTime <= 0)
            {
                talkerText.gameObject.SetActive(false);
                runTimer = false;
                remainTime = disappearDelay;
            }
        }
    }

    public void Say(string text)
    {
        talkerText.gameObject.SetActive(true);
        talkerText.text = text;
        runTimer = true;
    }

}