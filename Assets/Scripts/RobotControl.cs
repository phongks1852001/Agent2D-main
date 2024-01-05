using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class RobotControl : MonoBehaviour
{
    public LineRenderer line;
    [SerializeField] private GameObject gantry, magnet, currentHold, target, previoushold, midpoint;
    [SerializeField] private Collider2D bar;
    public float timer, magnetSpeed, gantrySpeed;
    public bool magnetDown, magnetUp, moveGantry, moveGantryWithAction;
    
    public string temptag;
    private float timerCount;

    public HashSet<GameObject> redbox, robot, finish;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        line.SetPosition(0, gantry.transform.position);
        line.SetPosition(1, magnet.transform.position);
        // Cu moi n giay, chay mot lan
        if (timerCount < 0)
        {
            timerCount = timer;
            scanEnvironment();
            Decide();
        }
        else
        {
            timerCount -= Time.deltaTime;
        }
        if (magnetDown)
        {
            magnet.transform.localPosition += Vector3.down * Time.deltaTime * magnetSpeed;
        }
        if (magnetUp && magnet.transform.localPosition.y <= -1.2f)
        {
            magnet.transform.localPosition += Vector3.up * Time.deltaTime * magnetSpeed;
        }
        else
        {
            magnetUp = false;
        }
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            magnetOFF(currentHold);
        }
        if (moveGantryWithAction)
        {
            Vector3 direction = gantry.transform.position;
            direction.x = Mathf.MoveTowards(direction.x, target.transform.position.x, Time.deltaTime * gantrySpeed);
            gantry.transform.position = direction;

            // Kiem tra xem da den target chua
            if (gantry.transform.position.x == target.transform.position.x)
            {
                Debug.Log("den noi");
                moveGantryWithAction = false;
                if (currentHold == null)
                {
                    magnetDown = true;
                }
                else
                {
                    magnetOFF(currentHold);
                }
            }
        }
        if (moveGantry)
        {
            Vector3 direction = gantry.transform.position;
            direction.x = Mathf.MoveTowards(direction.x, target.transform.position.x, Time.deltaTime*gantrySpeed);
            gantry.transform.position = direction;
            
            // Kiem tra xem da den target chua
            if (gantry.transform.position.x == target.transform.position.x)
            {
                Debug.Log("den noi");
                moveGantry = false;
            }
        }
        // Gioi han di chuyen cua gantry
        if (gantry.transform.localPosition.x >= 0.5f)
        {
            Vector3 position = gantry.transform.localPosition;
            position.x = 0.5f;
            gantry.transform.localPosition = position;
            Debug.Log("Gioi han max");
            moveGantry= false;
            moveGantryWithAction = false;
            if (currentHold != null)
            {
                magnetOFF(currentHold);
            }
        }
        if (gantry.transform.localPosition.x <= -0.5f)
        {
            Vector3 position = gantry.transform.localPosition;
            position.x = -0.5f;
            gantry.transform.localPosition = position;
            Debug.Log("Gioi han min");
            moveGantry= false;
            moveGantryWithAction = false;
            if (currentHold != null)
            {
                magnetOFF(currentHold);
            }
        }
    }
    private void magnetON(GameObject obj)
    {
        // Nhat box
        obj.transform.parent = magnet.transform;
        obj.GetComponent<Rigidbody2D>().isKinematic = true;
        currentHold = obj;
        temptag = obj.tag;
        obj.layer = LayerMask.NameToLayer("Robot");
        obj.tag = "Untagged";
    }
    private void magnetOFF(GameObject obj)
    {
        // Tha box
        if(obj == null) return;
        obj.transform.SetParent(null, true);
        obj.GetComponent<Rigidbody2D>().isKinematic = false;
        obj.tag = temptag;
        previoushold = obj;
        temptag = "";
        obj.layer = LayerMask.NameToLayer("Box");
        currentHold = null;
        StartCoroutine(WaitThenReturn(1f));
    }
    private void scanEnvironment()
    {
        redbox = GameObject.FindGameObjectsWithTag("RedBox").ToHashSet();
        robot = GameObject.FindGameObjectsWithTag("Robot").ToHashSet();
        finish = GameObject.FindGameObjectsWithTag("Finish").ToHashSet();
        //Debug.Log("RedBox: " + redbox.Count);
    }
    private void Decide()
    {
        if(moveGantry || moveGantryWithAction)
        {
            return;
        }
        // Check xem tay co empty?
        if (currentHold == null)
        {
            target = distanceFinder(redbox, 5f);
            if (target != null)
            {
                moveGantryWithAction = true;
            }
            else
            {
                moveGantryWithAction = false;
            }
        }
        else
        {
            // Tay dang cam do, quyet dinh lam gi
            if(distanceFinder(finish,5f) != null)
            {
                if (Vector3.Distance(transform.position, distanceFinder(finish, 5f).transform.position) < 10f)
                {
                    target = distanceFinder(finish, 5f);
                    moveGantryWithAction = true;
                }
            }
            else
            {
                if (distanceFinder(robotFinder(), 10f) != null)
                {
                    if (Vector3.Distance(midpoint.transform.position, distanceFinder(robotFinder(), 10f).transform.position) < 10f)
                    {
                        target = distanceFinder(robotFinder(), 10f);
                        moveGantryWithAction = true;
                    }
                }
                else
                {
                    // Khong co robot, khong co finish point
                    magnetOFF(currentHold);
                }
            }
            
        }
    }
    private GameObject distanceFinder(HashSet<GameObject> set, float searchdistance)
    {
        GameObject result = null;
        foreach (GameObject obj in set)
        {
            if (obj == midpoint || obj == previoushold) { continue; }
            if(Vector3.Distance(midpoint.transform.position, obj.transform.position) < searchdistance)
            {
                result = obj;
            }
        }
        return result;
    }
    private HashSet<GameObject> robotFinder()
    {
        HashSet<GameObject> set = new HashSet<GameObject>();
        float distance = Vector3.Distance(midpoint.transform.position, distanceFinder(finish, Mathf.Infinity).transform.position);
        foreach (GameObject obj in robot)
        {
            if (Vector3.Distance(obj.transform.position, distanceFinder(finish, Mathf.Infinity).transform.position) < distance)
            {
                set.Add(obj);
            }
        }
        return set;
    }
    private void OnTriggerEnter2D (Collider2D other)
    {
        if (other.gameObject.CompareTag("RedBox"))
        {
            magnetDown = false;
            magnetON(other.gameObject);
            magnetUp = true;
        }
        if (other.gameObject.CompareTag("Ground"))
        {
            magnetDown = false;
            magnetUp = true;
        }
    }
    private IEnumerator WaitThenReturn(float time)
    {
        yield return new WaitForSeconds(time);
        target = midpoint;
        moveGantry = true;
    }
}
