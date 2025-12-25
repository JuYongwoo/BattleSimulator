using UnityEngine;

public class PA_spawner : MonoBehaviour
{
	public bool spawned = false;

	[SerializeField]
	private int MPoint;
	[SerializeField]
	private float waitTime = 4f;
	private Area_tmp tmp;
	private int rand;


	void Awake()
	{
		Destroy(gameObject, waitTime);
		tmp = GameObject.FindGameObjectWithTag("Areas").GetComponent<Area_tmp>();

		Invoke(nameof(Spawn), 0.1f);
	}

	void Spawn()
	{
		if(spawned == false){
			if(MPoint == 1){
				rand = Random.Range(0, tmp.bRooms.Length);
				Instantiate(tmp.bRooms[rand], transform.position, tmp.bRooms[rand].transform.rotation);
			} else if(MPoint == 2){
				rand = Random.Range(0, tmp.fRooms.Length);
				Instantiate(tmp.fRooms[rand], transform.position, tmp.fRooms[rand].transform.rotation);
			} else if(MPoint == 3){
				rand = Random.Range(0, tmp.lRooms.Length);
				Instantiate(tmp.lRooms[rand], transform.position, tmp.lRooms[rand].transform.rotation);
			} else if(MPoint == 4){
				rand = Random.Range(0, tmp.rRooms.Length);
				Instantiate(tmp.rRooms[rand], transform.position, tmp.rRooms[rand].transform.rotation);
			}

			spawned = true;
		}
	}

	void OnTriggerEnter(Collider col)
	{
		if(col.CompareTag("SpawnPoint"))
		{
			if(!spawned)
			{
				Instantiate(tmp.empty_PA, transform.position, Quaternion.identity);

				spawned = true;

				Destroy(gameObject);
			} 
		}
	}
}
