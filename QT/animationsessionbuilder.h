#ifndef ANIMATIONSESSIONBUILDER_H
#define ANIMATIONSESSIONBUILDER_H

#define NOERR 0
#define ER_INVALID_DIMENSIONS 1
#define ER_INVALID_FILE_PATH 2
#define ER_NONMATCHING_DIMENSIONS 3

#include<map>
#include<guiddef.h>
#include<QBasics.h>
#include<animframeobject.h>
#include<OpenGLSessionManager.h>

class AnimationSessionBuilder : public QAbstractListModel {
    friend class MainWindow;

public:
    static AnimationSessionBuilder* getInstance();
    AnimFrameObject *getImage(QString name) const;
    QModelIndex getRoot() const;
    boolean activeSession() const;
    errno_t loadSystemImages();
    errno_t loadFrames(QList<QUrl> urls);

    //QAbstractItemModel functions
    QVariant data(const QModelIndex &index, int role = Qt::DisplayRole) const override;
    int rowCount(const QModelIndex &parent = QModelIndex()) const override;

private:
    AnimationSessionBuilder();
    static AnimationSessionBuilder* instance;
    errno_t loadImage(AnimFrameObject **frameObj, QString filePath, QString sessionIdentifier, ImgObjType type) const;
    void generateGrayScaleImages() const;

    // variables
    AnimFrameObject* drag_and_drop;
    QList<AnimFrameObject*>* frame_objects;
    QModelIndex root;

};
#endif // ANIMATIONSESSIONBUILDER_H
