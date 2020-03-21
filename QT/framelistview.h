#ifndef FRAMELISTVIEW_H
#define FRAMELISTVIEW_H

#define ER_VIEW_DOES_NOT_EXIST 1

#include<QListView>
#include<QBasics.h>
#include<animationsessionbuilder.h>

class FrameListView : public QListView
{
    Q_OBJECT

public:
    FrameListView(QWidget *parent);
    void initialize();
    AnimFrameObject* getSelection();

    static FrameListView* getInstance();

private slots:
    void currentChanged(const QModelIndex &current, const QModelIndex &previous) override;

signals:
    void onSelectionChanged();

private:
    AnimationSessionBuilder *animSession() { return AnimationSessionBuilder::getInstance(); }
    void indexesMoved(const QModelIndexList &indexes);

    static FrameListView* instance;
};

#endif // FRAMELISTVIEW_H
