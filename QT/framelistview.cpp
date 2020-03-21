#include "framelistview.h"

FrameListView* FrameListView::instance;

FrameListView* FrameListView::getInstance() {
    if(instance == nullptr){
        throw ER_VIEW_DOES_NOT_EXIST;
    }
    return instance;
}

FrameListView::FrameListView(QWidget *parent) : QListView(parent){
    instance = this;
}

AnimFrameObject* FrameListView::getSelection(){
    if(!currentIndex().isValid()){
        setCurrentIndex(animSession()->getRoot());
        update();
    }
    return animSession()->getImage(currentIndex().data(Qt::WhatsThisRole).toString());
}

void FrameListView::initialize(){
    auto oldModel = this->model();
    auto oldDelegate = this->itemDelegate();

    this->setViewMode(QListView::ViewMode::IconMode);
    QSize grid = QSize(this->geometry().height(), this->geometry().height());
    this->setSpacing(0);
    this->setGridSize(grid);
    this->setModel(animSession());
    this->setItemDelegate(new AnimFrameObjectDelegate(this));

    delete(oldDelegate);
    delete(oldModel);
}

void FrameListView::indexesMoved(const QModelIndexList &indexes) {
    qInfo() << "moved caught";
}

void FrameListView::currentChanged(const QModelIndex &current, const QModelIndex &previous) {
    this->QListView::currentChanged(current, previous);
    emit onSelectionChanged();
}

