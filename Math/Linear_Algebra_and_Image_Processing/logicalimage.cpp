#include "logicalimage.h"
#include "QDir"

using namespace std;


/*
 * The curve inspector's public constructor takes a pixel_map mapping points to LogicalPixels
 * and a start point. The inspect() call then returns a new CurveInspector that has found the root
 * of the longest viable path associated with that start point.
 *
 * Attached to that inspector will be an inspection_queue filled with other CurveInspectors.
 * If the inspect() method is called on each of them, they will identify additional curves
 * which can be marshalled by the logical image to generate LogicalLines.
 *
 * The public interface is very simple, and this class is designed to be consumed by the LogicalImage class.
 */

// public interface

LogicalPixel* LogicalPixel::_default = new LogicalPixel();

const float CurveInspector::viable_thresh = .08f;
CurveInspector::CurveInspector(const QImage& image, const Point start, map<Point, LogicalPixel*>& pixel_map)
                              : image(image), target(start), pixel_map(pixel_map), mode(Regular), collect_inspections(false),
                                start_direction(D::none), approximate_propogation(Vec3::zero){
    points.push_back(target);
    auto pix = pixel_map.at(target);
    pix->under_examination = true;
}

// copy constructor
CurveInspector::CurveInspector(const CurveInspector &other)
                              : image(other.image), target(other.target), pixel_map(other.pixel_map), mode(other.mode), collect_inspections(other.collect_inspections),
                                start_direction(other.start_direction), approximate_propogation(other.approximate_propogation) {
    last_direction = other.last_direction;
    length = other.length;
    longest_run = other.longest_run;
    current_run = other.current_run;
    volatility = other.volatility;
    on_end_point = other.on_end_point;
    inspection_queue = other.inspection_queue;
    points = other.points;
    viable_curve = other.viable_curve;
}

CurveInspector CurveInspector::operator=(const CurveInspector &inspector){
    return CurveInspector(inspector);
}

void CurveInspector::freePixelsFromExamination(list<Point> exemptions) const{
    for(auto it = points.begin(); it != points.end(); it++){
        auto found = find(exemptions.begin(), exemptions.end(), *it);
        auto end = exemptions.end();
        if(found == end){
            auto pix = pixel_map.at(*it);
            pix->under_examination = false;
        }
    }
}

const queue<CurveInspector, list<CurveInspector>> CurveInspector::getInspectionQueue() const{
    return inspection_queue;
}

const std::list<Point> CurveInspector::getPoints() const{
    return points;
}

const int CurveInspector::approximateDiscreteCurves() const{
    return qRound(volatility * 5); // arbitrary value, will adjust with testing
}

// private constructor includes more specific state options

CurveInspector::CurveInspector(const QImage& image, const Point start, std::map<Point, LogicalPixel*>& pixel_map,
                               InspectionMode mode, Direction start_direction, Vec3 approximate_propogation, bool collect_inspections, list<Point> exempt_points)
                              : image(image), target(start), pixel_map(pixel_map), mode(mode), collect_inspections(collect_inspections),
                                start_direction(start_direction), approximate_propogation(approximate_propogation), exempt_points(exempt_points){
    points.push_back(start);
    auto pix = pixel_map.at(target);
    pix->under_examination = true;
}

CurveInspector CurveInspector::inspect(){
    switch(mode){
    case Regular:
        return regularInspection();
    case StraightLine:
        return straightLineInspection();
    }

    return *this;
}

// recurses until a full path is identified
CurveInspector CurveInspector::regularInspection(){
    LMat<LogicalPixel*> neighborhood = getPixelNeighborhood(target);

    switch(start_direction){
        case Direction::none:{

            // for directionless examinations, we must find the deepest viable path
            if(last_direction == D::none){
                return locateAndTakeDeepestPath(neighborhood);
            }

            // standard examinations
            else {
                auto easiest_paths = approximate_propogation.getDirectionHierarchy();
                queue<DirectionData, list<DirectionData>> viable_paths = getViablePathQueue(neighborhood, easiest_paths, viable_thresh);

                // simple, singular path forward
                if(viable_paths.size() == 1 || (viable_paths.size() > 0 && !collect_inspections)){
                    auto direction_data = viable_paths.front();
                    viable_paths.pop();
                    if(takePath(direction_data))
                        return regularInspection();
                    else
                        return *this;
                }

                // multiple paths forward
                else if (viable_paths.size() > 1){
                    return findAndTraversePreferablePath(neighborhood, viable_paths);
                }

                // preferable path appended or no path forward, inspection complete
                return *this;
            }
        }

        default: {
            // starting along the path
            auto viable_paths = getViablePathQueue(neighborhood, Vec3::getVector(start_direction).getDirectionHierarchy(), .98);
            auto pix_data = viable_paths.front();
            if(pix_data.pix->associated_lines.size()){
                viable_curve = false;
                return *this;
            }
            takePath( pix_data );
            on_end_point = false;
            start_direction = Direction::none;

            return regularInspection();
        }
    }
}

CurveInspector CurveInspector::straightLineInspection(){
    switch(start_direction){
    case Direction::none:
        return *this;
    default:
        LMat<LogicalPixel*> neighborhood = getPixelNeighborhood(target);
        LogicalPixel* pix = getPixelInDirection(neighborhood, start_direction);
        if(pix->is_valid){
            current_run += 1;
            longest_run = current_run;
            points.insert(points.end(), target);
        } else {
            start_direction = Direction::none;
        }
        target = getPointInDirection(target, start_direction);
        return straightLineInspection();
    }
}

CurveInspector CurveInspector::locateAndTakeDeepestPath(const LMat<LogicalPixel*> neighborhood){
    auto viable_paths = getViablePathQueue(neighborhood, approximate_propogation.getDirectionHierarchy(), 0); // return any and all paths

    vector<CurveInspector> curves;
    list<Point> exemptions = exempt_points;
    exemptions.insert(exemptions.end(), points.begin(), points.end());

    while(!viable_paths.empty()){
        auto data = viable_paths.front();
        CurveInspector inspector = CurveInspector(image, target, pixel_map, Regular, data.direction, Vec3::zero, false, exemptions).inspect();
        inspector.freePixelsFromExamination(exempt_points);
        curves.push_back(inspector);
        viable_paths.pop();
    }

    sort(curves.begin(), curves.end(), [](CurveInspector a, CurveInspector b) -> bool {
       return a && b ? a.length > b.length : a;
    });

    auto longest_c = curves.begin();
    if(longest_c != curves.end() && *longest_c){
        freePixelsFromExamination(); // free any internal pixels from inspection
        return CurveInspector(image, *(--(*longest_c).points.end()), pixel_map, Regular, (*longest_c).last_direction).inspect();
    } else {
        viable_curve = false;
        return *this;
    }
}


CurveInspector CurveInspector::findAndTraversePreferablePath(const LMat<LogicalPixel*> neighborhood, queue<DirectionData, list<DirectionData>> &viable_paths){

    vector<pair<Direction, CurveInspector>> curves;
    list<Point> exemptions = exempt_points;
    exemptions.insert(exemptions.end(), points.begin(), points.end());

    while(!viable_paths.empty()){
        auto data = viable_paths.front();
        Vec3 approx_prop = ((Vec3::getVector(data.direction) + approximate_propogation) / 2).norm();
        CurveInspector inspector(image, target, pixel_map, Regular, data.direction, approx_prop, false, exemptions);
        curves.push_back({ data.direction, inspector.inspect() });
        inspector.freePixelsFromExamination(exemptions);
        viable_paths.pop();
    }

    sort(curves.begin(), curves.end(), [](pair<Direction, CurveInspector> a, pair<Direction, CurveInspector> b) -> bool {
       return a.second.preferabilityMetric() > b.second.preferabilityMetric();
    });

    auto it = curves.begin();
    append((*it).second);

    while(++it != curves.end()){
        auto pix = getPixelInDirection(neighborhood, (*it).first);
        pix->flagged_for_inspection = true;
        inspection_queue.push(CurveInspector(image, target, pixel_map, Regular, (*it).first));
    }

    return *this;
}

queue<DirectionData, list<DirectionData>> CurveInspector::getViablePathQueue
    (LMat<LogicalPixel*> neighborhood, const vector<pair<Direction, float>> ordered_directions, float threshold){

    queue<DirectionData, list<DirectionData>> paths_forward;

    for(auto pair : ordered_directions){

        LogicalPixel* pix = getPixelInDirection(neighborhood, pair.first);

        if(pix->is_valid){
            if(pair.second >= threshold) {
                paths_forward.push({ pix, pair.first, pair.second });

            } else if(collect_inspections && !pix->associated_lines.size() && !pix->under_examination && !pix->flagged_for_inspection){
                // register a potential path for subsequent inspection
                pix->flagged_for_inspection = true;
                inspection_queue.push(CurveInspector(image, getPointInDirection(target, pair.first), pixel_map, Regular));
            }
        }
    }
    return paths_forward;
}

// returns true if path is taken, returns false if we've reached the end
bool CurveInspector::takePath(DirectionData candidate){
    bool any_lines = candidate.pix->associated_lines.size() || candidate.pix->under_examination;
    if(!on_end_point || (on_end_point && !any_lines)){
        auto pix = candidate.pix;
        pix->under_examination = true;

        // update point data
        target = getPointInDirection(target, candidate.direction);
        points.push_back(target);
        on_end_point = any_lines;
        length += 1;

        // update curve metrics
        last_direction = D::opposite(candidate.direction);
        volatility += qPow(1 - candidate.cos_th, 2);
        Vec3 approx(((Vec3::getVector(candidate.direction) + approximate_propogation) / 2).norm());
        approximate_propogation = approx;
        return true;
    } else {
        return false;
    }
}

double CurveInspector::preferabilityMetric() const {
    return length - (volatility * 5); // hard-coded value is arbitrary, will adjust with testing
}

void CurveInspector::appendInspectionQueue(CurveInspector &other){
    while(!other.inspection_queue.empty()){
        auto curve = other.inspection_queue.front();
        inspection_queue.push(curve);
        other.inspection_queue.pop();
    }
}

void CurveInspector::append(CurveInspector &other){
    for(auto it = other.points.begin()++; it != other.points.end(); it++){
        points.push_back(*it);
    }
    volatility += other.volatility;
}

const Point CurveInspector::getPointInDirection(const Point point, Direction direction) const{
    switch(direction) {
    case D::u:
        return { point.x, point.y - 1 };
    case D::u_r:
        return { point.x + 1, point.y - 1 };
    case D::r:
        return { point.x + 1, point.y };
    case D::d_r:
        return { point.x + 1, point.y + 1 };
    case D::d:
        return { point.x, point.y + 1 };
    case D::d_l:
        return {point.x - 1, point.y + 1 };
    case D::l:
        return { point.x -1, point.y };
    case D::u_l:
        return { point.x - 1, point.y - 1 };
    default:
        return point;
    }
}

LogicalPixel* CurveInspector::getPixelInDirection(LMat<LogicalPixel*> neighborhood, Direction direction) const{
    switch(direction) {
    case D::u:
        return neighborhood(0, 1);
    case D::u_r:
        return neighborhood(0, 2);
    case D::r:
        return neighborhood(1, 2);
    case D::d_r:
        return neighborhood(2, 2);
    case D::d:
        return neighborhood(2, 1);
    case D::d_l:
        return neighborhood(2, 0);
    case D::l:
        return neighborhood(1, 0);
    case D::u_l:
        return neighborhood(0, 0);
    default:
        return neighborhood(1, 1);
    }
}

LMat<LogicalPixel*> CurveInspector::getPixelNeighborhood(Point point) const{
    LMat<LogicalPixel*> neighborhood(3, 3);
    for(int i = -1; i < 2; i++){
        for(int j = -1; j < 2; j++){
            int x = point.x + (j);
            int y = point.y + (i);
            if(x < 0 || y < 0 || x >= image.width() || y >= image.height()){
                neighborhood(i + 1, j + 1) = LogicalPixel::_default;
            } else {
                auto it = pixel_map.find({ x, y });
                //LogicalPixel pix =
                neighborhood(i + 1, j + 1) = it != pixel_map.end() ? pixel_map.at({ x, y }) : LogicalPixel::_default;
            }
        }
    }
    return neighborhood;
}

/*
 * LogicalCurve
 */

LogicalCurve::LogicalCurve(uint16_t line_id, CurveInspector inspector, const map<Point, LogicalPixel*>& pixel_map)
    : line_id(line_id), ordered_points(inspector.getPoints()), discrete_curves(inspector.approximateDiscreteCurves()){
    for(auto point : ordered_points){
        LogicalPixel* pix = pixel_map.at(point);
        pix->associated_lines.push_back(line_id);
    }
    inspector.freePixelsFromExamination();
}

const std::list<Point> LogicalCurve::getPoints() const{
    return ordered_points;
}

/*
 * The LogicalImage class creates a map of logical pixels with Points as the key value.
 * The LogicalImage then consumes the CurveInspector to marshall those data into discrete curves.
 * Those curves are then stored as LogicalCurves, and each LogicalPixel is given a foreign 'key' (integer)
 * representing the curve(s) it's associated with.
 */

LogicalImage::LogicalImage(const QImage* underlying_image): underlying_image(underlying_image){
    assert(underlying_image->format() == QImage::Format::Format_Grayscale8);
    createLogicalPixels(underlying_image);
    marshallLogicalPixelsToCurves();
}

QImage* LogicalImage::writeColorsToNewImage() const{
    vector<QRgb> colors;
    colors.push_back(0xFF0000FF);
    colors.push_back(0xFFFF0000);
    colors.push_back(0xFF00FFFF);
    colors.push_back(0xFFFFFF00);
    colors.push_back(0xFFFF00FF);
    colors.push_back(0xFF00FF00);
    colors.push_back(0xFFFFFFFF);
    auto it = colors.begin();

    QImage* test = new QImage(*underlying_image);
    test->convertTo(QImage::Format::Format_ARGB32);
    for(auto curve : curve_map){
        QRgb color = *it;
        it += 1;
        it = it == colors.end() ? colors.begin() : it;
        for(auto point : curve.second.getPoints()){
            test->setPixel(point.x, point.y, color);
        }
    }
    return test;
}

void LogicalImage::createLogicalPixels(const QImage* underlying_image){
    for(int m = 0; m < underlying_image->height(); m++){
        const uint8_t* scanline = static_cast<const uint8_t*>(underlying_image->scanLine(m));
        for(int n = 0; n < underlying_image->width(); n++){
            if(scanline[n]){
                Point point { n, m }; // x, y
                std::pair<Point, LogicalPixel*> pair(point, new LogicalPixel(point));
                pixel_map.insert(pair);
            }
        }
    }
}

void LogicalImage::marshallLogicalPixelsToCurves(){
    LogicalPixel* unbound_pixel = getUnboundLogicalPixel();

    while(unbound_pixel->is_valid){
        CurveInspector inspected_curve = CurveInspector(*underlying_image, unbound_pixel->point, pixel_map).inspect();
        int index = inspected_curve ? curve_index++ : -1;
        curve_map.insert({ index, LogicalCurve(index, inspected_curve, pixel_map)});

        queue<CurveInspector, list<CurveInspector>> insp_queue = inspected_curve.getInspectionQueue();
        list<CurveInspector> underlying_list = insp_queue._Get_container();

        while(!insp_queue.empty()){
            CurveInspector queue_insp_curve = insp_queue.front().inspect();
            index = queue_insp_curve ? curve_index++ : -1;
            curve_map.insert({ index, LogicalCurve(index, queue_insp_curve, pixel_map)});
            insp_queue.pop();

            list<CurveInspector> new_queue_elements = queue_insp_curve.getInspectionQueue()._Get_container();
            underlying_list.insert(underlying_list.end(), new_queue_elements.begin(), new_queue_elements.end());
        }

        unbound_pixel = getUnboundLogicalPixel();
    }
}

// return the first unbound logical pixel in the map
LogicalPixel* LogicalImage::getUnboundLogicalPixel() const {
    for(auto pair : pixel_map){
        LogicalPixel* pix = pair.second;
        if(!pix->associated_lines.size()){
            return pix;
        }
    }
    return LogicalPixel::_default;
}
