#include <cmath>
#include <functional>
#include "LowerLevelLinearFunctions.h"

using namespace LLinearAlgebra;

Vec3 Vec3::upLeft = Vec3(-1, 1, 0).norm();
Vec3 Vec3::up = Vec3(0, 1, 0);
Vec3 Vec3::upRight = Vec3(1, 1, 0).norm();
Vec3 Vec3::right = Vec3(1, 0, 0);
Vec3 Vec3::downRight = Vec3(1, -1, 0).norm();
Vec3 Vec3::down = Vec3(0, -1, 0);
Vec3 Vec3::downLeft = Vec3(-1, -1, 0).norm();
Vec3 Vec3::left = Vec3(-1, 0, 0);
Vec3 Vec3::zero = Vec3(0, 0, 0);
Vec3 Vec3::out = Vec3(0, 0, 1);

int LLinearAlgebra::PartialFactorial(const int target, const int iterations) {
    if (target == 0) {
        return 1;
    }

    int retval = 1;
    for (int i = target; i > target - iterations; i--) {
        retval *= i;
    }

    return retval;
}

double LLinearAlgebra::NChooseK(const int n, const int k) {
    if (!(n >= 0 && k >= 0)) {
            std::perror("nChooseK was passed a negative integer in FunctionTester");
            std::abort();
    }

    return (double)PartialFactorial(n, k) / (double)PartialFactorial(k, k);
}
