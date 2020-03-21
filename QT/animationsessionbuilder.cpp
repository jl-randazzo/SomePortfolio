#include "animationsessionbuilder.h"

// AnimationSessionBuilder

AnimationSessionBuilder* AnimationSessionBuilder::instance;

AnimationSessionBuilder::AnimationSessionBuilder() {
    frame_objects = new QList<AnimFrameObject*>;
}

AnimationSessionBuilder* AnimationSessionBuilder::getInstance() {
    if (instance == nullptr) {
        instance = new AnimationSessionBuilder();
    }
    return instance;
}

boolean AnimationSessionBuilder::activeSession() const{
    return frame_objects->size() > 0;
}

AnimFrameObject * AnimationSessionBuilder::getImage(QString name) const{
    if(name == "dragAndDropIcon")
        return drag_and_drop;

    AnimFrameObject* image =
            findFirstInQList<AnimFrameObject*>(frame_objects, [=](AnimFrameObject* item) -> bool { return item->name == name; });
    return image;
}

QModelIndex AnimationSessionBuilder::getRoot() const{
    return root;
}

errno_t AnimationSessionBuilder::loadSystemImages(){
    return loadImage(&drag_and_drop, ":/GUIImages/Dragndrop.png", "dragAndDropIcon", NonFrame);
}

errno_t AnimationSessionBuilder::loadImage(AnimFrameObject **frameObj, QString filePath, QString sessionIdentifier, ImgObjType obj_type = NonFrame) const {
    QImage * img = new QImage(filePath);
    if(img == nullptr || img->isNull()){
        qInfo() << "FAILED TO LOAD IMAGE AT PATH: " << filePath;
        return ER_INVALID_FILE_PATH;
    }

    *frameObj = new AnimFrameObject(sessionIdentifier, img, obj_type);

    return NOERR;
}

errno_t AnimationSessionBuilder::loadFrames(QList<QUrl> urls) {
    QString frame = "frame";
    QSize prevSize;
    ImgObjType type = FirstFrame;
    int index = 0;

    beginInsertRows(QModelIndex(), 0, urls.size());
    for(QUrl url : urls){
        QString frame_name = frame + QString::number(index);
        AnimFrameObject *frame_obj;
        errno_t err = loadImage(&frame_obj, url.toLocalFile(), frame_name, type);
        if(err != NOERR){
            return err;
        }

        QSize imgSize = frame_obj->sprite->size();
        if(!prevSize.isEmpty() && imgSize != prevSize){
            qInfo() << "IMAGE AT PATH: " << url.url() << " DOES NOT MATCH THE DIMENSIONS OF PRECEDING FRAMES";
            return ER_NONMATCHING_DIMENSIONS;
        }

        QModelIndex indexObj = createIndex(0, index, frame_obj);
        if(!root.isValid()){
            root = indexObj;
        }

        prevSize = imgSize;

        type = SubsequentFrame;
        frame_objects->push_back(frame_obj);
        index++;
    }

    insertRows(frame_objects->size(), urls.size(), root);
    endInsertRows();

    return NOERR;
}

void AnimationSessionBuilder::generateGrayScaleImages() const {
    for(AnimFrameObject * frame : *frame_objects){
        frame->generateGrayScale();
    }
}

// overrides from base implementation follow

QVariant AnimationSessionBuilder::data(const QModelIndex &index, int role) const {
    if(index.isValid()){
        switch(role){
        case Qt::DecorationRole:
            return QVariant(frame_objects->at(index.row())->thumbnail);
        case Qt::ToolTipRole:
            return index.row() == 0 ? "Primary Frame" : QVariant();
        case Qt::WhatsThisRole:
            return frame_objects->at(index.row())->name;
        default:
            break;
        }
    }

    return QVariant();
}

int AnimationSessionBuilder::rowCount(const QModelIndex &parent) const {
    return frame_objects->size();
}
