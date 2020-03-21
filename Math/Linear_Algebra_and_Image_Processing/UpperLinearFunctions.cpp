#include <cmath>
#include"LowerLevelLinearFunctions.h"
#include"UpperLinearFunctions.h"

using namespace std;

namespace LLinearAlgebra {

function<double(double)>* bernsteinPolynomials(const int n) {
    function<double(double)> * bernsteinFuncs = new function<double(double)>[n + 1];

    for (int i = 0; i <= n; i++) {
        bernsteinFuncs[i] = [=](double t) -> double {
            return (NChooseK(n, i) * pow(t, i) * pow((1 - t), n - i));
        };
    }

    return bernsteinFuncs;
}

}
