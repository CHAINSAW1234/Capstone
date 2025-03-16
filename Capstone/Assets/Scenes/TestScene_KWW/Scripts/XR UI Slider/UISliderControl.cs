using UnityEngine;
using UnityEngine.UI;

public class UISliderControl : MonoBehaviour
{
    [SerializeField] private Slider slider;
    [SerializeField] Light libraryLight;
    private float minValue = 0.0f;
    private float maxValue = 29.0f;
    private float xyBufferValue = 4.0f;

    private Vector3 startPos;
    private Vector3 lastPos;
    private Vector3 endPos;
    private bool moveBool;
    private GameObject _hand;
    private float speed = 1.0f;

    void Start()
    {
        startPos = transform.localPosition;
        endPos = new Vector3(maxValue, minValue, minValue);
    }

    void Update()
    {
        if (moveBool)
        {
            MoveSlider();          
        }     
    }
    private void MoveSlider()
    {
        lastPos = transform.localPosition;
        if (lastPos.x <= minValue)
        {
            transform.localPosition = startPos;
        }
        else if (lastPos.x >= maxValue)
        {
            transform.localPosition = endPos;
        }

        if (lastPos.z <= -xyBufferValue || lastPos.z >= xyBufferValue)
        {
            transform.localPosition = new Vector3(lastPos.x, 0, 0);
        }
        if (lastPos.y <= -xyBufferValue || lastPos.y >= xyBufferValue)
        {
            transform.localPosition = new Vector3(lastPos.x, 0, 0);
        }
        slider.value = lastPos.x;
        float step = speed * Time.deltaTime;
        transform.position = Vector3.MoveTowards(transform.position, _hand.transform.position, step);
        if (libraryLight != null)
        {
            libraryLight.intensity = slider.value;
        }
    }
    private void OnTriggerEnter(Collider other)
    {
        Debug.Log(other.gameObject.name);
        _hand = other.gameObject;
        moveBool = true;
    }

    private void OnTriggerExit(Collider other)
    {
        moveBool = false;
    }
}
