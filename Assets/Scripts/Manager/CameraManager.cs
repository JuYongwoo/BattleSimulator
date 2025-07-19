using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public class CameraManager
{
    public UIBase_CameraNameUI mUIBase_CameraNameUI = null;


    private List<Camera> cameras = new List<Camera>();
    private List<int> cameraOwnerIndex = new List<int>();
    private int currentCameraIndex = -1;

    public void Awake()
    {
        mUIBase_CameraNameUI = GameObject.FindFirstObjectByType<UIBase_CameraNameUI>(); //첫번째 GamaManager 스크립트를 가진 오브젝트를 찾는다. //GameManager는 하나만 존재한다

    }


    public void Start()
    {
        foreach (var obj in GameManager.mAll_Of_Game_Objects)
        {

            if (obj.Value.GetComponent<ObjectBase>().mObjectType == GameData.ObjectType.AI)
            {
                AddCameraToCharacter(obj.Value, obj.Value.GetComponent<ObjectBase>().mID); //AI들에게 카메라를 부착한다.

            }
        }
    }
    public void Update()
    {
        if (mUIBase_CameraNameUI != null) uiUpdate();



        // 스페이스바를 누를 때마다 카메라 전환 (카메라 관리)
        if (Input.GetKeyDown(KeyCode.Space))
        {
            // 현재 카메라를 비활성화
            if (currentCameraIndex != -1)
            {
                cameras[currentCameraIndex].enabled = false;
            }

            // 다음 카메라로 이동
            currentCameraIndex = (currentCameraIndex + 1) % cameras.Count;

            // 새 카메라 활성화
            cameras[currentCameraIndex].enabled = true;
        }
    }

    public void uiUpdate()
    {

        //중앙 하단 현재 카메라 캐릭터 이름
        if (currentCameraIndex != -1)
        {
            mUIBase_CameraNameUI.GetComponent<Text>().text = "";
            mUIBase_CameraNameUI.GetComponent<Text>().text += GameManager.mAll_Of_Game_Objects[cameraOwnerIndex[currentCameraIndex]].name;
        }




    }


    public void AddCameraToCharacter(GameObject character, int pID)
    {
        // 캐릭터에 카메라 생성 및 설정
        GameObject cameraGameObject = new GameObject("CharacterCamera");
        Camera cam = cameraGameObject.AddComponent<Camera>();
        cam.transform.SetParent(character.transform);
        cam.transform.localPosition = new Vector3(0, 2.5f, 0); // 머리 위에 위치
        cam.transform.rotation *= Quaternion.Euler(20, 0, 0);

        // 카메라 리스트에 추가
        cameras.Add(cam);
        cameraOwnerIndex.Add(pID);
        // 카메라 초기 비활성화
        cam.enabled = false;
    }


}
