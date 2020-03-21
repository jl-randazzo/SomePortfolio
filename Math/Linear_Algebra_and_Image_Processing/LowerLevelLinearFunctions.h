#ifndef LOWERLEVELLINEARFUNCTIONS_H
#define LOWERLEVELLINEARFUNCTIONS_H
#include<stdarg.h>

#define TRACK_INSTANCES(classname, array) \
    ~classname(){ \
    *instances -= 1; \
        if(*instances <= 0){ \
            delete[] array;\
            delete instances;\
        }\
    }

#include<QtMath>
#include<vector>
#include<list>
#include<queue>
#include"QBasics.h"

namespace LLinearAlgebra{

typedef class Direction {
public:
    enum Val {
        none, u, u_r, r, d_r, d, d_l, l, u_l
    };

    constexpr Direction(Val val): val(val){}
    operator Val() const { return val; }
    explicit operator bool() = delete;

    constexpr bool operator == (Direction other) { return val == other.val; }
    constexpr bool operator == (Val other) { return val == other; }
    constexpr bool operator != (Direction other) { return val != other.val; }
    constexpr bool operator != (Val other) { return val != other; }

     static Direction opposite(const Direction dir){
        switch(dir){
        case u:
            return d;
        case u_r:
            return d_l;
        case r:
            return l;
        case d_r:
            return u_l;
        case d:
            return u;
        case d_l:
            return u_r;
        case l:
            return r;
        case u_l:
            return d_r;
        default:
            return none;
        }
    }

private:
    Val val;
} D;


struct Vec3{
    static Vec3 upLeft;
    static Vec3 up;
    static Vec3 upRight;
    static Vec3 right;
    static Vec3 downRight;
    static Vec3 down;
    static Vec3 downLeft;
    static Vec3 left;
    static Vec3 zero;
    static Vec3 out;

    Vec3(): x(0), y(0), z(0){}
    Vec3(double x, double y, double z): x(x), y(y), z(z){}
    double x;
    double y;
    double z;

    Vec3(const Vec3 &other): x(other.x), y(other.y), z(other.z){}
    Vec3(const int &i): x(i), y(i), z(i){}

    float dot(const Vec3 &other) const{
        return x * other.x + y * other.y + z * other.z;
    }
    Vec3 cross(const Vec3 &other) const{
        return Vec3(y * other.z - z * other.y, -(x * other.z - z * other.x), x * other.y - y * other.x);
    }
    float length() const{
        return qSqrt(dot(*this));
    }
    Vec3 norm() const{
        float length = this->length();
        return Vec3(x / length, y / length, z / length);
    }
    Vec3 weightedAverage(const Vec3 &other, int weight){
        assert(weight >= 1);
        return Vec3( (x + other.x) / weight, (y + other.y) / weight, (z + other.z) / weight);
    }
    Vec3 operator+ (const Vec3 &other) const{
        return Vec3(other.x + x, other.y + y, other.z + z);
    }
    Vec3 operator* (double scale) const{
        return Vec3(x * scale, y * scale, z * scale);
    }
    Vec3 operator / (float dividend) const{
        return Vec3(x / dividend, y / dividend, z / dividend);
    }
    void operator= (const Vec3 &other) {
        x = other.x;
        y = other.y;
        z = other.z;
    }

    static Vec3 getVector(Direction dir){
        switch(dir){
        case D::u:
            return up;
        case D::u_r:
            return upRight;
        case D::r:
            return right;
        case D::d_r:
            return downRight;
        case D::d:
            return down;
        case D::d_l:
            return downLeft;
        case D::l:
            return left;
        case D::u_l:
            return upLeft;
        default:
            return zero;
        }
    }

    std::vector<std::pair<Direction, float>> getDirectionHierarchy(){
        std::vector<std::pair<Direction, float>> amounts;
        amounts.push_back(std::pair<Direction, float>(D::u, dot(up)));
        amounts.push_back(std::pair<Direction, float>(D::u_r, dot(upRight)));
        amounts.push_back(std::pair<Direction, float>(D::r, dot(right)));
        amounts.push_back(std::pair<Direction, float>(D::d_r, dot(downRight)));
        amounts.push_back(std::pair<Direction, float>(D::d, dot(down)));
        amounts.push_back(std::pair<Direction, float>(D::d_l, dot(downLeft)));
        amounts.push_back(std::pair<Direction, float>(D::l, dot(left)));
        amounts.push_back(std::pair<Direction, float>(D::u_l, dot(upLeft)));

        sort(amounts.begin(), amounts.end(), [](std::pair<Direction, float> a, std::pair<Direction, float> b) -> bool{
            return a.second > b.second;
        });

        return amounts;
    }
};

struct RowOperation{
    enum type { Swap, Mul, Add };
    const type _type;
    const int from_row;
    const int to_row;
    const double coefficient;
};

class Reduceable{
    friend class LAugMat;

public:
    Reduceable(int height, int width): height(height), width(width){}
    Reduceable(const Reduceable& other): height(other.height), width(other.width){}
    const int height;
    const int width;

private:
    virtual void rref(std::queue<RowOperation, std::list<RowOperation>> operations) = 0;
    virtual std::queue<RowOperation, std::list<RowOperation>> rref() = 0;
    virtual Reduceable* dup() const = 0;
};

template <class T>
constexpr bool is_pointer = std::is_pointer<T>();

// Abstract n-dimensional vector initialized with external values
template <class T>
class AbsVecn: public Reduceable{
public:
    AbsVecn(const T* const data, int n, int leap_size = 1): data(new T[n]), Reduceable(n, 1), instances(new int(1)){
        for(int m = 0; m < n; m++){
            this->data[m] = data[m * leap_size];
        }
    }

    AbsVecn(int n, int leap_size = 1): data(new T[n]), Reduceable(n, 1), instances(new int(1)){}

    AbsVecn(const AbsVecn &other): data(other.data), Reduceable(other.height, other.width), instances(other.instances){
        *instances += 1;
    }

    TRACK_INSTANCES(AbsVecn, data)

    template <class V>
    V dot(const AbsVecn<V>& other) const{
        assert(height == other.height);
        V sum(0);
        for(int i = 0; i < height; i++){
            if constexpr(is_pointer<T>){
                sum += other(i) * (int)data[i];
            } else {
                V added = sum + (other(i) * operator()(i));
                sum = sum + (other(i) * operator()(i));
            }
        }
        return sum;
    }

    T& operator() (int ind) const {
        assert(ind < height);
        return data[ind];
    }

    AbsVecn operator + (const AbsVecn& other) const {
        AbsVecn new_vec(this->data, height, 1);
        for(int i = 0; i < height; i++){
            if constexpr(is_pointer<T>){
                new_vec(i) += (int)other(i);
            } else {
                new_vec(i) += other(i);
            }
        }
        return new_vec;
    }

    AbsVecn operator * (const double& coeff) const{
        AbsVecn new_vec(data, height, 1);
        for(int i = 0; i < height; i++){
            if constexpr(is_pointer<T>){
                new_vec(i) = (T)((int)new_vec(i) * (int)coeff);
            } else {
                new_vec(i) = new_vec(i) * coeff;
            }
        }
        return new_vec;
    }

private:
    T* const data;
    int* const instances;

    void performOperation(RowOperation op){
        T from_val;
        if constexpr(is_pointer<T>){
            from_val = (T)(int)operator()(op.from_row);
        } else {
            from_val = operator()(op.from_row);
        }

        switch(op._type){
        case RowOperation::Swap:{
            T temp = operator()(op.from_row);
            operator()(op.from_row) = operator()(op.to_row);
            operator()(op.to_row) = temp;
            break;
        }
        case RowOperation::Mul:{
            if constexpr(is_pointer<T>){
                T temp = (T)(int)((int)from_val * op.coefficient);
                operator()(op.to_row) = temp;
            } else {
                T temp = from_val * op.coefficient;
                operator()(op.to_row) = temp;
            }
            break;
        }
        case RowOperation::Add:{
            if constexpr(is_pointer<T>){
                T temp = (T)(int)((int)from_val * (int)op.coefficient);
                operator()(op.to_row) = operator()(op.to_row) + (int)temp;
            } else {
                T temp = from_val * op.coefficient;
                operator()(op.to_row) = operator()(op.to_row) + temp;
            }
            break;
        }
        }
    }

    Reduceable* dup() const override {
        AbsVecn* vec = new AbsVecn(data, height, 1);
        return vec;
    }

    std::queue<RowOperation, std::list<RowOperation>> rref() override{
        return std::queue<RowOperation, std::list<RowOperation>>();
    }

    void rref(std::queue<RowOperation, std::list<RowOperation>> operations) override{
        while(!operations.empty()){
            performOperation(operations.front());
            operations.pop();
        }
    }
};

template <class T>
class LMat: public Reduceable{
private:
  T* matrix;
  int* instances;

public:
  LMat(const int m, const int n): Reduceable(m, n) {
      matrix = new T[m * n];
      instances = new int(1);
      //
  }

  LMat(const int m, const int n, T* data): matrix(data), Reduceable(m, n){
      instances = new int(2); // the underlying data
  }

  LMat(const LMat &other): Reduceable(other.height, other.width) {
      matrix = other.matrix;
      instances = other.instances;
      *instances += 1;
  }

  TRACK_INSTANCES(LMat, matrix);

    T& operator() (int m, int n) const{
        int index = width * m + n;
        return matrix[index];
    }

    void setRow(const int replacement_index, const AbsVecn<T>& new_row){
        assert(new_row.height == width && replacement_index < height);
        for(int i = 0; i < width; i++){
            T& ind = operator()(replacement_index, i);
            T data = new_row(i);
            operator()(replacement_index, i) = new_row(i);
        }
    }

    void setColumn(const int replacement_index, const AbsVecn<T>& new_col){
        assert(new_col.height == height && replacement_index < width);
        for(int i = 0; i < height; i++){
            operator()(i, replacement_index) = new_col(i);
        }
    }

    const AbsVecn<T> getRow(int m) const{
        assert(m < height);
        const AbsVecn<T> ret(&operator()(m, 0), width, 1);
        return ret;
    }

    const AbsVecn<T> getColumn(int n) const{
        assert(n < width);
        const AbsVecn<T> ret(&operator()(0, n), height, width);
        return ret;
    }

    LMat transpose() const{
        LMat transpose(width, height);
        for(int m = 0; m < height; m++){
            transpose.setColumn(m, getRow(m));
        }
        return transpose;
    }

    LMat duplicate() const{
        LMat dup(height, width);
        for(int m = 0; m < height; m++){
            for(int n = 0; n < width; n++){
                dup(m, n) = operator()(m, n);
            }
        }
        return dup;
    }

    template <class V>
    LMat<V> operator* (const LMat<V> other){
        assert(width == other.height);
        LMat<V> result(height, other.width);
        for(int m = 0; m < height; m++){
            AbsVecn row = getRow(m);
            for(int n = 0; n < other.width; n++){
                result(m, n) = row.dot(other.getColumn(n));
            }
        }
        return result;
    }

    template <class V>
    AbsVecn<V> operator* (const AbsVecn<V>& vector){
        assert(width == vector.height);
        AbsVecn<V> result(height, 1);
        for(int i = 0; i < height; i++){
            result(i) = this->getRow(i).dot(vector);
        }
        return result;
    }

private:
    void performOperation(RowOperation op){
        switch(op._type){
        case RowOperation::Swap:{
            AbsVecn<T> temp = getRow(op.from_row);
            setRow(op.from_row, getRow(op.to_row));
            setRow(op.to_row, temp);
            break;
        }
        case RowOperation::Mul:{
            AbsVecn<T> temp = getRow(op.from_row) * op.coefficient;
            setRow(op.to_row, temp);
            break;
        }
        case RowOperation::Add:{
            AbsVecn<T> temp = getRow(op.from_row) * op.coefficient;
            setRow(op.to_row, getRow(op.to_row) + temp);
            break;
        }
        }
    }

    int findLeadingOneAtIndex(int i){
        for(int m = i; m < height; m++){
            if(operator()(m, i) == (T)(1))
                return m;
        }
        return -1;
    }

    int findValueAtIndex(int i){
        for(int m = i; m < height; m++){
            if(operator()(m, i) != 0){
                return i;
            }
        }
        return -1;
    }

    void zeroRestOfColumn(std::queue<RowOperation, std::list<RowOperation>>& queue, const int diagonal_ind){
        // subsequent and then preceding
        int m = diagonal_ind + 1 == height ? 0 : diagonal_ind + 1;
        while((m < height && m > diagonal_ind) || m < diagonal_ind ){
            T& val = operator()(m, diagonal_ind);
            if(val != 0){
                if constexpr(is_pointer<T>){
                    RowOperation op{RowOperation::Add, diagonal_ind, m, -(int)val };
                    queue.push(op);
                    performOperation(op);
                } else {
                    RowOperation op{RowOperation::Add, diagonal_ind, m, -val };
                    queue.push(op);
                    performOperation(op);
                }
            }
            m = (m == (height - 1) ? 0 : m + 1);
        }
    }

    inline double getDoubleValue(int m, int n){
        double val;
        if constexpr(is_pointer<T>){
            val = (int)operator()(m, n);
        } else {
            val = operator()(m, n);
        }
        return val;
    }

    void noLeadingOnesRoutine(std::queue<RowOperation, std::list<RowOperation>>& operations, const int diagonal_ind){
        double val = getDoubleValue(diagonal_ind, diagonal_ind);

        if(operator()(diagonal_ind, diagonal_ind) != 0){
            RowOperation mul{RowOperation::Mul, diagonal_ind, diagonal_ind, 1.0 / val };
            operations.push(mul);
            performOperation(mul);
            zeroRestOfColumn(operations, diagonal_ind);
        } else {
            int value_ind = findValueAtIndex(diagonal_ind);
            auto location = value_ind == -1 ? NOT_FOUND : GREATER;
            if(location == NOT_FOUND)
                return;
            else{
                RowOperation swap{ RowOperation::Swap, value_ind, diagonal_ind, 0 };
                operations.push(swap);
                performOperation(swap);
                val = getDoubleValue(diagonal_ind, diagonal_ind); // value has changed from swap
                RowOperation mul{RowOperation::Mul, diagonal_ind, diagonal_ind, 1.0 / val };
                operations.push(mul);
                performOperation(mul);
                zeroRestOfColumn(operations, diagonal_ind);
            }
        }
    }

    enum { SAME, GREATER, NOT_FOUND };
    std::queue<RowOperation, std::list<RowOperation>> rref() override{
        std::queue<RowOperation, std::list<RowOperation>> operations;
        int max_degree = height > width ? width : height;

        for(int i = 0; i < max_degree; i++){
            int leading_one_ind = findLeadingOneAtIndex(i);
            auto location = leading_one_ind == -1 ? NOT_FOUND : leading_one_ind == i ? SAME : GREATER;

            switch(location){
            case GREATER:{
                RowOperation swap{RowOperation::Swap, leading_one_ind, i, 0};
                operations.push(swap);
                performOperation(swap);
            } // 'break' intentionally left out because 'SAME' routine applies after swap
            case SAME:{
                zeroRestOfColumn(operations, i);
                break;
            }
            case NOT_FOUND:{
                noLeadingOnesRoutine(operations, i);
                break;
            }
            }
        }

        return operations;
    }

    Reduceable * dup() const override{
        LMat* mat = new LMat(duplicate());
        return mat;
    }

    void rref(std::queue<RowOperation, std::list<RowOperation>> operations) override{
        while(!operations.empty()){
            performOperation(operations.front());
            operations.pop();
        }
    }
};

/*
 *
 *
 */
class LAugMat {
public:
    LAugMat(const Reduceable& A, const Reduceable& b): A(A.dup()), b(b.dup()), instances(new int(0)){
        assert(this->A->height == this->b->height);
    }
    LAugMat(const LAugMat& other): A(other.A), b(other.b), instances(other.instances){
        *instances += 1;
    }

    ~LAugMat(){
        *instances -= 1;
        if(*instances <= 0){
            delete instances;
            delete A;
            delete b;
        }
    }

    Reduceable& rowReduction(){
        auto operations = A->rref();
        b->rref(operations);
        return *b;
    }

    const Reduceable* getA() const{
        return A;
    }

private:
    Reduceable* const A;
    Reduceable* const b;
    int* const instances;

};

int PartialFactorial(const int target, const int iterations);
double NChooseK(const int n, const int k);
}

#endif // LOWERLEVELLINEARFUNCTIONS_H
