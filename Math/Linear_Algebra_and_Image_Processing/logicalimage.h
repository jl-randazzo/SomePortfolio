#ifndef LOGICALIMAGE_H
#define LOGICALIMAGE_H
#include "QBasics.h"
#include<map>
#include<utility>
#include<QGenericMatrix>
#include<QtMath>
#include<list>
#include<queue>
#include "LowerLevelLinearFunctions.h"

using namespace LLinearAlgebra;

struct Point{
    Point() { x = -1; y = -1; }
    Point(int x, int y): x(x), y(y){}
    int x;
    int y;

    int manhattanLength() const{
        return qAbs(x) + qAbs(y);
    }
    int dot(const Point &other) const{
        return x * other.x + y * other.y;
    }
    bool operator< (const Point &other) const{
        return y == other.y ? x < other.x : y < other.y;
    }
    bool operator> (const Point &other) const{
        return y == other.y ? x > other.x : y > other.y;
    }
    bool operator== (const Point &other) const{
        return x == other.x && y == other.y;
    }
    explicit operator bool() const{
        return x >= 0 && y >= 0;
    }
};

struct LogicalPixel {

    static LogicalPixel* _default;

    LogicalPixel(): point({ -1, -1}), is_valid(false) {}
    LogicalPixel(Point point, bool val = true): point(point), is_valid(val){}
    bool under_examination = false;
    bool flagged_for_inspection = false;
    const Point point;
    const bool is_valid;
    std::vector<uint16_t> associated_lines;

    explicit operator bool() const { return is_valid; }

    bool operator < (const LogicalPixel &other) const{
        return point.manhattanLength() < other.point.manhattanLength();
    }
    bool operator > (const LogicalPixel &other) const{
        return point.manhattanLength() > other.point.manhattanLength();
    }
    bool operator == (const LogicalPixel &other) const{
        return point == other.point;
    }
    LogicalPixel operator =(const LogicalPixel &pixel){
        return LogicalPixel(pixel.point, pixel.is_valid);
    }

    // template satisfaction
    // scalar operations are illegal with LogicalPixel
    LogicalPixel(const double& val): point({ -1, -1 }), is_valid(false) {}
    operator double(){ // needed for LMat template, but can't be intantiated
        throw 1;
    }
};


struct DirectionData{
    LogicalPixel* pix;
    const Direction direction;
    const float cos_th;
};

class CurveInspector{
public:
    enum InspectionMode { Regular, StraightLine  };
    CurveInspector(const QImage& image, const Point start, std::map<Point, LogicalPixel*>& pixel_map);
    CurveInspector(const CurveInspector &inspector);
    CurveInspector operator =(const CurveInspector &inspector);

    CurveInspector inspect();

    void freePixelsFromExamination(std::list<Point> exemptions = std::list<Point>()) const;
    const std::queue<CurveInspector, std::list<CurveInspector>> getInspectionQueue() const;
    const std::list<Point> getPoints() const;
    const int approximateDiscreteCurves() const;

    operator bool() { return viable_curve; }

private:
    CurveInspector(const QImage& image, const Point start, std::map<Point, LogicalPixel*>& pixel_map,
                   InspectionMode mode, Direction start_direction = Direction::none, Vec3 approximate_propogation = Vec3::zero,
                   bool collect_inspections = true, std::list<Point> exempt_points = std::list<Point>());

    double preferabilityMetric() const;
    void appendInspectionQueue(CurveInspector &other);
    void append(CurveInspector &other);
    bool takePath(DirectionData candidate);
    std::queue<DirectionData, std::list<DirectionData>> getViablePathQueue
        (LMat<LogicalPixel*> neighborhood, const std::vector<std::pair<Direction, float>> ordered_directions, float threshold);

    CurveInspector regularInspection();
    CurveInspector straightLineInspection();

    CurveInspector locateAndTakeDeepestPath(const LMat<LogicalPixel*> neighborhood);
    CurveInspector findAndTraversePreferablePath(const LMat<LogicalPixel*> neighborhood, std::queue<DirectionData, std::list<DirectionData>> &viable_paths);

    LogicalPixel* getPixelInDirection(LMat<LogicalPixel*> neighborhood, Direction direction) const;
    const Point getPointInDirection(Point point, Direction direction) const;
    LMat<LogicalPixel*> getPixelNeighborhood(Point point) const;

    // immutable data
    const QImage& image;
    std::map<Point, LogicalPixel*>& pixel_map;
    const InspectionMode mode;
    const bool collect_inspections;
    static const float viable_thresh; // roughly 75 degrees or 3pi/8, viable path forward

    // session data
    // point data
    Point target;
    std::list<Point> points;
    std::queue<CurveInspector, std::list<CurveInspector>> inspection_queue;
    bool on_end_point = false;
    std::list<Point> exempt_points;

    // curve metrics
    Vec3 approximate_propogation;
    Direction start_direction;
    Direction last_direction = D::none;
    double volatility = 0;
    int length = 0;
    bool viable_curve = true;
    int longest_run = 0;
    int current_run = 0;
};

class LogicalCurve {
public:
    LogicalCurve(uint16_t line_id, CurveInspector inspector, const std::map<Point, LogicalPixel*>& pixel_map);
    const std::list<Point> getPoints() const;

private:
    const uint16_t line_id;
    const double discrete_curves;
    const std::list<Point> ordered_points;

    bool operator < (const LogicalCurve &other) const {
        return line_id < other.line_id;
    }
    bool operator > (const LogicalCurve &other) const {
        return line_id > other.line_id;
    }
    bool operator == (const LogicalCurve &other) const {
        return line_id == other.line_id;
    }
};

class LogicalImage
{
public:
    LogicalImage(const QImage* underlying_image);
    QImage* writeColorsToNewImage() const;

private:
    void createLogicalPixels(const QImage* underlying_image);
    void marshallLogicalPixelsToCurves();
    LogicalPixel* getUnboundLogicalPixel() const;
    //void
    std::map<Point, LogicalPixel*> pixel_map;
    std::map<uint16_t, LogicalCurve> curve_map;
    uint16_t curve_index = 0;
    const QImage* underlying_image;
};

#endif // LOGICALIMAGE_H
