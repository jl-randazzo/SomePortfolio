import java.io.File;
import java.io.IOException;
import java.util.*;
import java.io.FileWriter;

public class LukeBinaryTree3 {

    public Node root;
    public int balances, totalNodes;

    public LukeBinaryTree3(){}

    /*
     * I decided to go with a left/right/parent structure. I used a balance of recursion and loops for all the operations.
     */

    private class Node{
        Node left, right, parent;
        String key;
        Comparable item; //this is the value

        public Node(String key, Comparable item, Node parent){
            this.key = key;
            this.item = item;
            this.parent = parent;
        }
    }

    //can be used in a static context that doesn't have access to the Node.
    public Comparable search(String key){
        Node x = get(key);
        if(x != null) return x.item;
        else return null;
    }

    //simple recursive size method.
    public int size(Node x){
        if(x==null)return 0;
        return(size(x.left)+size(x.right)+1);
    }

    //minLength method finds the shortest path to a null link. Uses loops and recursion.
    public int minLength(Node x){
        if(x==null)return 0;

        int minLength = totalNodes; //arbitrarily large

        int distance = 0, tempDist = 0;
        for(Node y = x; y != null; y = y.left){
            distance++;
            tempDist = distance + minLength(y.right);
            //else tempDist = distance;
            if(minLength > tempDist)minLength = tempDist;
            if(minLength == 1) return minLength;
        }

        distance = 0;
        for(Node y = x; y != null; y = y.right){
            distance++;
            tempDist = distance + minLength(y.left);
            //else tempDist = distance;
            if(minLength > tempDist)minLength = tempDist;
            if(minLength == 1) return minLength;
        }
        return minLength;
    }

    //Part of the benefit of having a parent Node is that it made it pretty easy to have a non-recursive add method
    //Pretty straightforward, I think. Saves the depth and passes it as an argument to my balancing machine.
    public void add(Comparable item){
        totalNodes++;

        if(root==null){root = new Node(item.key, item, null); return;}

        else{
            Node x = root, y = null;
            int cmp = 0, depth = 0;
            while(x != null && !x.key.equals(item.key)){
                y = x;
                cmp = x.key.compareTo(item.key);
                if(cmp < 0)x = x.right;
                else x = x.left;
                depth++;
            }

            x = new Node (item.key, item, y);
            if(cmp < 0) y.right = x;
            else y.left = x;
            if(x.parent!=root)x = orient(x); //orient method makes sure the leaves aren't pointing inward

            theBalancingMachine(x.parent, x, depth, 1);
        }
    }

    /*
    * To put it simply, the balancingMachine climbs the tree and makes sure there aren't any paths along the way
    * more than two shorter than the most recently added leaf. If there is, it calls the balance Method.
    */

    public void theBalancingMachine(Node relRoot, Node x, int depth, int i){
        Node y = x;
        int minLength, j = 2;

        while(i <= depth){
            if(y == relRoot.right){
                minLength = minLength(relRoot.left);

                if(minLength <= i - j){

                    if(relRoot.parent!=null) {
                        Node z = relRoot.parent;
                        balances++;
                        if (relRoot == z.right) {z.right = balance(relRoot); relRoot = z.right;}
                        else {z.left = balance(relRoot); relRoot = z.left;}
                    }
                    else {
                        balances++;
                        root = balance(root);
                    }

                }
            }

            else if(y == relRoot.left){
                minLength = minLength(relRoot.right);

                if(minLength <= i - j){

                    if(relRoot.parent!=null) {
                        Node z = relRoot.parent;
                        balances++;
                        if (relRoot == z.right) {z.right = balance(relRoot); relRoot = z.right;}
                        else {z.left = balance(relRoot); relRoot = z.left;}
                    }
                    else {
                        balances++;
                        root = balance(root);
                    }

                }
            }
            y = relRoot;
            relRoot = relRoot.parent;
            i++;
        }
    }




    //once again, this just makes sure that the new leaf isn't pointing inward if it doesn't have a sibling
    public Node orient(Node x){
        if(x.parent.parent.left!=null && x == x.parent.parent.left.right && x.parent.parent.left.left == null)
        {x.parent.parent.left = rotateLeft(x.parent); x = x.left;}
        else if(x.parent.parent.right!=null && x == x.parent.parent.right.left && x.parent.parent.right.right == null)
        {x.parent.parent.right = rotateRight(x.parent); x = x.right;}

        return x;
    }



    /*
     * My balance algorithm uses a couple of algorithms to make sure I've found the best term to head the subtree/tree
     * It then uses recursion to balance the right/left subtrees for each link.
     */
    public Node balance(Node q){
        if(q == null) return null;
        int i = size(q);
        if(i == 1) return q;

        //these are arbitrary terms, the max # of nodes a tree of height 2, 3, 4, 5, 6, 7, and 8.
        //In these situations, we know that both sides of the tree should have the same # of items
        //I use the while loops to determine which side is bigger and then I rotate the first link
        //in that direction until it doesn't have an inward pointing term. I repeat the process until
        //both sides are the same size. I then call the function on the left and right links and return the
        //root of the subtree.
        if(i == 7 || i == 15 || i == 31 || i == 63 || i == 127 || i == 255 || i ==511){
            while(size(q.right) < size(q.left)){
                while(q.left.right!=null)q.left=rotateLeft(q.left);
                q = rotateRight(q);
            }
            while(size(q.left) < size(q.right)){
                while(q.right.left!=null)q.right=rotateRight(q.right);
                q = rotateLeft(q);
            }
            q.left = balance(q.left); q.right = balance(q.right); return q;
        }

        //These loops test if one side is too much larger than the other and then I perform rotations
        //until the size disparity is not out of line with a balanced tree. The inner loops are there to make
        //sure that the rotations won't make the side growing from the rotations aren't going to get bigger than the other side.
        //once it's all done, I call the function on the left and right limbs and return the head of the subtree.
        while(size(q.left) * 2 + 1 < size(q.right)) {
            while (size(q.left) + size(q.right.left) + 1 > size(q.right.right)) {
                if (q.right.left != null) q.right = rotateRight(q.right);
            }
            q = rotateLeft(q);
        }
        while(size(q.right) * 2 + 1 < size(q.left)) {
            while (size(q.right) + size(q.left.right) + 1 > size(q.left.left)) {
                if (q.left.right != null) q.left = rotateLeft(q.left);
            }
            q = rotateRight(q);
        }

        q.left = balance(q.left); q.right = balance(q.right); return q;
    }


    /*
     * Rotations were a bit harder to implement than expected for a left/right/parent implementation. The if statements are
     * there to protect against null pointer exceptions. Making x = null at the end is redundant, but it was helpful during
     * debugging because I was able to diagnose that my rotate methods were creating duplicates of nodes and trees that
     * ascended in multiple directions.
     */
    public Node rotateLeft(Node x){
        Node y = new Node (x.key, x.item, x.parent);
        y.right = x.right;
        y.left = x.left;
        if(x.left!=null)x.left.parent = y;
        x.right.parent = y.parent;
        y.right = x.right.left;
        if(x.right.left!=null) x.right.left.parent=y;
        x.right.left = y;
        y.parent = x.right;
        x = null;
        return y.parent;
    }

    public Node rotateRight(Node x){
        Node y = new Node (x.key, x.item, x.parent);
        y.right = x.right;
        y.left = x.left;
        if(x.right!=null)x.right.parent = y;
        x.left.parent = y.parent;
        y.left = x.left.right;
        if(x.left.right!=null) x.left.right.parent=y;
        x.left.right = y;
        y.parent = x.left;
        x = null;
        return y.parent;
    }

    /*
     * the get and getDepth methods return the Node with a given key and the depth of the Node with a given key
     * respectively.
     */
    public Node get(String key){
        if(root==null)return null;
        if(root.key.equals(key)){/*parentGetter = null;*/ return root;}
        else return get(key, root);
    }

    public Node get(String key, Node x){
        if(x.key.equals(key))return x;
        if(x == null)return null;
        int cmp = x.key.compareTo(key);
        if(cmp > 0) return get(key, x.left);
        else return get(key, x.right);
    }

    public int getDepth(String key){
        if(root==null)return 0;
        if(root.key.equals(key)){/*parentGetter = null;*/ return 0;}
        else return getDepth(key, root);
    }

    public int getDepth(String key, Node x){
        if(x.key.equals(key))return 1;
        if(x == null)return 0;
        int cmp = x.key.compareTo(key);
        if(cmp > 0) return 1 + getDepth(key, x.left);
        else return 1 + getDepth(key, x.right);
    }

    //Uses type boolean to select the output algorithm I'm using. Pretty handy.
    public void output(boolean selector, FileWriter fw) throws IOException{
        if(selector)preOrder(fw);
        else inOrder(fw);
    }

    public void preOrder (FileWriter fw) throws IOException{
        fw.write("PREORDER OUTPUT:\n\n");
        if(root!=null) preOrder(root, fw);
    }

    public void preOrder(Node current, FileWriter fw) throws IOException {
        if(current==null)return;
        fw.write(current.item.toString() +"\n");
        preOrder(current.left, fw);
        preOrder(current.right, fw);
    }

    public void inOrder (FileWriter fw) throws IOException{
        fw.write("INORDER OUTPUT:\n\n");
        if(root!=null) inOrder(root, fw);
    }

    public void inOrder(Node current, FileWriter fw) throws IOException {
        if(current==null)return;
        inOrder(current.left, fw);
        fw.write(current.item.toString() + "\n");
        inOrder(current.right, fw);
    }


    /*
     * For the part of the program that was reasonably easy to conceptualize, this sure took forever to implement
     * Let me know if you have questions. I've tested it and it works, just a long method.
     */
    public void delete(String key) {
        Node temp = get(key);
        if(temp==null) return;
        totalNodes--;
        int minDepth = getDepth(key);

        if (temp.left == null) {
            if (temp.right == null) temp = null;
            else {
                if (temp == temp.parent.right) {
                    temp.parent.right = temp.right;
                    temp.right.parent = temp.parent;
                    return;
                }
                else {
                    temp.parent.left = temp.right;
                    temp.right.parent = temp.parent;
                    return;
                }
            }
        } else if (temp.right == null) {
            if (temp == temp.parent.right) {
                temp.parent.right = temp.left;
                temp.left.parent = temp.parent;
                return;
            }
            else {
                temp.parent.left = temp.left;
                temp.left.parent = temp.parent;
                return;
            }
        } else if (size(temp.left)<size(temp.right)){
            while(temp.right.left!=null)temp.right = rotateRight(temp.right);
            if (temp == temp.parent.right) {
                temp.parent.right = temp.right;
                temp.right.parent = temp.parent;
                temp.right.left = temp.left;
                temp.left.parent = temp.right;
                temp.parent.right = balance(temp.parent.right);
                temp = temp.parent.right;
                minDepth = minDepth + minLength(temp);
                theBalancingMachine(temp.parent, temp, minDepth - 1, minLength(temp.parent));
            }
            else {
                temp.parent.left = temp.right;
                temp.right.parent = temp.parent;
                temp.right.left = temp.left;
                temp.left.parent = temp.right;
                temp.parent.left = balance(temp.parent.left);
                temp = temp.parent.left;
                minDepth = minDepth + minLength(temp);
                theBalancingMachine(temp.parent, temp, minDepth - 1, minLength(temp.parent));
            }
        } else {
            while(temp.left.right!=null)temp.left = rotateLeft(temp.left);
            if (temp == temp.parent.right) {
                temp.parent.right = temp.left;
                temp.left.parent = temp.parent;
                temp.left.right = temp.right;
                temp.right.parent = temp.left;
                temp.parent.right = balance(temp.parent.right); balances++;
                temp = temp.parent.right;
                minDepth = minDepth + minLength(temp);
                theBalancingMachine(temp.parent, temp, minDepth - 1, minLength(temp.parent));
            }
            else {
                temp.parent.left = temp.left;
                temp.left.parent = temp.parent;
                temp.left.right = temp.right;
                temp.right.parent = temp.left;
                temp.parent.left = balance(temp.parent.left); balances++;
                temp = temp.parent.left;
                minDepth = minDepth + minLength(temp);
                theBalancingMachine(temp.parent, temp, minDepth - 1, minLength(temp.parent));
            }
        }
        balance(root);
    }


    //if you want to use this, feel free. It prints the minimum length of the path from the head of the subtrees
    //on the left and right. Not super helpful, but I was using it to make sure the tree was balanced.

    public String totalMinLength(){
        return minLength(root.left) + " " + minLength(root.right) + "";
    }
}
